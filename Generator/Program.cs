// See https://aka.ms/new-console-template for more information
using HexaGen;
using HexaGen.Patching;

var files = Directory.GetFiles("include", "*.h", SearchOption.AllDirectories).ToList();
GeneratorBuilder.Create<CsCodeGenerator>("generator.json")
    .WithPrePatch(new NamingPatch(["HexaUtils"], NamingPatchOptions.CaseInsensitive))
    .Generate("include/utils.h", "../../../../Hexa.NET.Utilities/Native/", files);