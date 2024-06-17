using System;
using System.Collections.Generic;
using CommandLine;

namespace MovieListcompnCLI
{
    public class Options
    {
        [Option('f', "first-list", Required = true, HelpText = "The file path to the first list.")]
        public string firstList { get; set; }

        [Option('s', "second-list", Required = true, HelpText = "The file path to the second list.")]
        public string secondList { get; set; }

        [Option('a', "algorithm", Default = "direct", HelpText = "The name of the comparison algorithm. Must be one of the following: direct, defaultratio, partialratio, tokenset, partialtokenset, tokensort, partialtokensort, tokenabbreviation, partialtokenabbreviation, weighted.")]
        public string algorithm { get; set; }

        [Option('c', "cutoff", Default = 75, HelpText = "The cutoff for the passed fuzzy comparison algorithm.")]
        public int cutoff { get; set; }

        [Option('d', "directory", Default = ".", HelpText = "The destination directory for the generated 'matches.txt', 'unmatched.txt' and 'collisions.txt' files.")]
        public string directory { get; set; }
    }
}
