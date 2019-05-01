﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using OData2Poco.CustAttributes;
using OData2Poco.TextTransform;

namespace OData2Poco
{
    /// <summary>
    ///     Generate c# code
    /// </summary>
    internal class PocoClassGeneratorCs : IPocoClassGenerator
    {
        string nl=Environment.NewLine;

        public string LangName { get; set; } = "csharp";
        public List<ClassTemplate> ClassList { get; set; } 
        private static IPocoGenerator _pocoGen;
        private static string CodeText { get; set; }
        public PocoSetting PocoSetting { get; set; }
        bool blankSpaceBeforeProperties = true;
        //key is fullName: <namespace.className>
        public ClassTemplate this[string key] => ClassList.FirstOrDefault(x => x.FullName == key);

        internal string Header;

        //container for all classes
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="pocoGen"></param>
        /// <param name="setting"></param>
        public PocoClassGeneratorCs(IPocoGenerator pocoGen, PocoSetting setting = null)
        {
            PocoSetting = setting ?? new PocoSetting();
            _pocoGen = pocoGen;
            //add jsonproperty to properties/classes that are renamed
            PocoSetting?.Attributes.Add("original"); //v3.2

            //initialize AttributeFactory to use pocosetting.Attributes
            AttributeFactory.Default.Init(PocoSetting);

            ClassList = _pocoGen.GeneratePocoList();
            //check reserved keywords
            ModelManager.RenameClasses(ClassList);
            Header = GetHeader() ?? "";
            CodeText = null;
        }

        /// <summary>
        ///     Generate C# code for all POCO classes in the model
        /// </summary>
        /// <returns></returns>
        public string GeneratePoco()
        {
            //var ns = PocoModel.Select(x => x.Value.NameSpace).Distinct()
            //    .OrderBy(x => x).ToList();
            var ns = ClassList.Select(x => x.NameSpace).Distinct()
                .OrderBy(x => x).ToList();
            var template = new FluentCsTextTemplate { Header = Header };

            template.WriteLine(UsingAssemply(ns));
            foreach (var s in ns)
            {

                //Use a user supplied namespace prefix combined with the schema namepace or just the schema namespace

                var namespc = PrefixNamespace(s);
                template.StartNamespace(namespc);
                var pocoModel2 = ClassList.Where(x => x.NameSpace == s);
                foreach (var item in pocoModel2)
                {
                    template.WriteLine(ClassToString(item)); //c# code of the class
                }
                template.EndNamespace();
            }
            return template.ToString();
        }

        internal string PrefixNamespace(string name)
        {
            string namespc = name;
            if (!string.IsNullOrWhiteSpace(PocoSetting.NamespacePrefix))
            {
                namespc = PocoSetting.NamespacePrefix + "." + name;
            }

            return namespc;
        }

        private string GetHeader()
        {
            var comment = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated using  OData2Poco System.
//     Service Url: {0}
//     MetaData Version: {1}
//     Generated On: {2}
// </auto-generated>
//------------------------------------------------------------------------------
//";

            //The <auto-generated> tag  at the start of the file
            var h = new FluentCsTextTemplate();
            h.WriteLine(comment, _pocoGen.MetaData.ServiceUrl, _pocoGen.MetaData.MetaDataVersion,
                DateTimeOffset.Now.ToString("s"));

            return h.ToString();
        }
        private string UsingAssemply(List<string> nameSpaces)
        {
            var h = new FluentCsTextTemplate();
            var assemplyManager = new AssemplyManager(PocoSetting, ClassList);
            var asemplyList = assemplyManager.AssemplyReference;
            foreach (var entry in asemplyList)
            {
                h.UsingNamespace(entry);
            }
            //add also namespaces of the built-in schema namespaces
            if (nameSpaces.Count > 1)
                nameSpaces.ForEach(x =>
                {
                    var namespc = PrefixNamespace(x);
                    h.UsingNamespace(namespc);
                });
            return h.ToString();
        }

        internal string ReducedBaseTyp(ClassTemplate ct)
        {
            var ns = $"{ct.NameSpace}."; //
            var reducedName = ct.BaseType;
            if (ct.BaseType.StartsWith(ns))
                reducedName = ct.BaseType.Replace(ns, "");
            return reducedName;
        }

        /// <summary>
        ///     Generte C# code for a given  Entity using FluentCsTextTemplate
        /// </summary>
        /// <param name="ent"> Class  to generate code</param>
        /// <param name="includeNamespace"></param>
        /// <returns></returns>
        internal string ClassToString(ClassTemplate ent, bool includeNamespace = false)
        {
            var csTemplate = new FluentCsTextTemplate();

            ////for enum
            if (ent.IsEnum)
            {
                
                var elements = string.Join($",{nl}", ent.EnumElements.ToArray());
                var flagAttribute = ent.IsFlags ? "[Flags] " : "";
                var enumString = $"\t{flagAttribute}public enum {ent.Name}{nl}\t {{{nl} {elements} {nl}\t}}";
                return enumString;
            }



            foreach (var item in ent.GetAllAttributes()) //not depend on pocosetting
            {
                csTemplate.PushIndent("\t").WriteLine(item).PopIndent();
            }
            var baseClass = ent.BaseType != null && PocoSetting.UseInheritance
                ? ReducedBaseTyp(ent) //ent.BaseType 
                : PocoSetting.Inherit;

            csTemplate.StartClass(ent.Name, baseClass, partial: true, abstractClass: ent.IsAbstrct);

            foreach (var p in ent.Properties)
            {
                var pp = new PropertyGenerator(p, PocoSetting);


                if (p.IsNavigate)
                {
                    if (!PocoSetting.AddNavigation && !PocoSetting.AddEager) continue;
                }


                foreach (var item in pp.GetAllAttributes())
                {
                    if (!string.IsNullOrEmpty(item))
                        csTemplate.WriteLine(item);
                }
                csTemplate.WriteLine(pp.Declaration);

                if (blankSpaceBeforeProperties)
                    csTemplate.WriteLine(""); //empty line
            }
            csTemplate.EndClass();
            if (includeNamespace) csTemplate.EndNamespace(); //"}" for namespace
            CodeText = csTemplate.ToString();
            return CodeText;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(CodeText)) CodeText = GeneratePoco();
            return CodeText;
        }
    }
}