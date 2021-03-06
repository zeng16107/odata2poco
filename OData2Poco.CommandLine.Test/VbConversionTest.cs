﻿
using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace OData2Poco.CommandLine.Test
{
    [TestFixture]
    public partial class ProgramTests  
    {

        [Test]
        public async Task vb_convert_Test()
        {
            var source = @"public class MyClas {}";
            var vbCode = await VbCodeConvertor.CodeConvert(source);
            Assert.That(vbCode, Does.Contain("Public Class MyClas"));
            Assert.That(vbCode, Does.Contain("End Class"));

        }

        [Test]
        public async Task vb_convert_attribute_Test()
        {
            var source = @"
[Table]
public class MyClas 
{
     [Key]
    public int Id {get;set;}
  public string Name {get;set;}
}";
         
            var vbCode = await VbCodeConvertor.CodeConvert(source);

            /*
           <Table>
           Public Class MyClas
               <Key>
               Public Property Id As Integer
               Public Property Name As String
           End Class
            */

            Assert.That(vbCode, Does.Contain("<Table>"));
            Assert.That(vbCode, Does.Contain("Public Class MyClas"));
            Assert.That(vbCode, Does.Contain(" Public Property Name As String"));
            Assert.That(vbCode, Does.Contain("End Class"));

        }

        [Test]
        public async Task vb_code_generation_end_to_end_Test()
        {
            var url = TestSample.NorthWindV4;
            var a = $"-r {url}  -v -a key req --lang vb";
            var tuble = await RunCommand(a);
            var output = tuble.Item2;

            Assert.That(output, Does.Contain("Public Partial Class Product"));
            Assert.That(output, Does.Contain("<Key>"));
            Assert.That(output, Does.Contain("<Required>"));
            Assert.That(output, Does.Contain(" Public Property ProductID As Integer"));
        }

        [Test]
        public async Task Lang_invalid_Test()
        {
            var url = TestSample.NorthWindV4;
            var a = $"-r {url}  -v --lang zz";
            var tuble = await RunCommand(a);
            var output = tuble.Item2;
            Assert.That(output, Does.Contain("Invalid Language Option 'zz'. It's set to 'cs'."));

        }

        [Test]
        public async Task Attribute_invalid_Test()
        {
            var url = TestSample.NorthWindV4;
            var a = $"-r {url}  -v -a zz";
            var tuble = await RunCommand(a);
            var output = tuble.Item2;
            Assert.That(output, Does.Contain("Attribute 'zz' isn't valid. It will be  droped"));
        }
 
    }
}

