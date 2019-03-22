using System;
using LandscapeBuilderLib;
using McMaster.Extensions.CommandLineUtils;

namespace LandscapeBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication();
            app.HelpOption();

            CommandOption landscapeNameOption = app.Option("-l|--landscape-name <LANDSCAPENAME>", "The name of the landscape being generated.", CommandOptionType.SingleValue);
            CommandOption outputDirOption = app.Option("-o|--output <OUTPUTDIR>", "The path where the intermediary output files will be written. If --output-to-condor is not used, the final outputs will also be written here.", CommandOptionType.SingleValue);
            CommandOption atlasDirOption = app.Option("-i|--input <INPUTDIR>", "The path to the map files the landscape will be generated from.", CommandOptionType.SingleValue);
            CommandOption genDDSOption = app.Option("-d|--gen-dds", "When using this option, the final DDS textures will be generated.", CommandOptionType.NoValue);
            CommandOption genForestOption = app.Option("-f|--gen-forest", "When using this option, the final .for forest files will be generated.", CommandOptionType.NoValue);
            CommandOption genThermalOption = app.Option("-t|--gen-thermal", "When using this option, the final .tdm thermal file will be generated.", CommandOptionType.NoValue);
            CommandOption genAllOption = app.Option("-A|--gen-all", "When using this option, the final DDS textures, forest files and thermal file will be genereated", CommandOptionType.NoValue);
            CommandOption outputToCondorOption = app.Option("-c|--output-to-condor", "When using this option, the final files will be written to the landscape's directory, defined by --landscape-name.", CommandOptionType.NoValue);
            CommandOption singleTileOption = app.Option("-s|--single-tile <TILENAME>", "The name of the single tile to be generated, in order to more quickly test settings changes", CommandOptionType.SingleValue);
            CommandOption qgisStringOption = app.Option("-Q|--qgis-string <WIDTH>,<HEIGHT>", "Returns the string that should be used when generating the tiles from QGIS based on the width and height values provided. All other arguments will be ignored if this flag is used", CommandOptionType.MultipleValue);

            app.OnExecute(() =>
            {
                LandscapeBuilderLib.LandscapeBuilder builder = new LandscapeBuilderLib.LandscapeBuilder();

                if (!qgisStringOption.HasValue())
                {
                    bool genDDS = genDDSOption.HasValue() || genAllOption.HasValue();
                    bool genForestFiles = genForestOption.HasValue() || genAllOption.HasValue();
                    bool genThermalFile = genThermalOption.HasValue() || genAllOption.HasValue();
                    bool outputToCondor = outputToCondorOption.HasValue();
                    string outputDir = outputDirOption.HasValue() ? outputDirOption.Value() : string.Empty;
                    string atlasDir = atlasDirOption.HasValue() ? atlasDirOption.Value() : string.Empty;
                    string singleTile = singleTileOption.HasValue() ? singleTileOption.Value() : string.Empty;

                    builder.Build(genDDS, genForestFiles, genThermalFile, outputToCondor, outputDir, atlasDir, singleTile);
                }
                else
                {
                    string[] arg = qgisStringOption.Value().Split(",");
                    
                    if(arg.Length == 2)
                    {
                        int width, height;
                        if(int.TryParse(arg[0], out width) && int.TryParse(arg[1], out height))
                        {
                            Console.Write(Utilities.GetAtlasString(width, height));
                        }
                    }
                }
            });

            app.Execute(args);

        }
    }
}
