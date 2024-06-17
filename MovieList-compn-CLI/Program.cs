using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using FuzzySharp;
using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer;
using FuzzySharp.SimilarityRatio.Scorer.Composite;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using MovieFileLibrary;

namespace MovieListcompnCLI
{
    class Program
    {
        /// <summary>
        /// The algorithms list.
        /// </summary>
        private static List<string> algorithmsList = new List<string>() { "direct", "defaultratio", "partialratio", "tokenset", "partialtokenset", "tokensort", "partialtokensort", "tokenabbreviation", "partialtokenabbreviation", "weighted" };

        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                /*.WithNotParsed(HandleParseError)*/;
        }

        /// <summary>
        /// Runs the options.
        /// </summary>
        /// <param name="options">Options.</param>
        static void RunOptions(Options options)
        {
            // Set last error
            List<string> errorList = new List<string>();

            /* Validate arguments */

            // Check first list path
            if (!File.Exists(options.firstList))
            {
                // Set error
                errorList.Add("First list path is invalid.");
            }

            // Check second list path
            if (!File.Exists(options.secondList))
            {
                // Set error
                errorList.Add("Second list path is invalid.");
            }

            // Check algorithm
            if (!algorithmsList.Contains(options.algorithm))
            {
                // Set error
                errorList.Add($"Invalid algorithm. Must be one of the following: {string.Join(", ", algorithmsList)}.");
            }

            // Check cutoff
            if (options.cutoff < 1 || options.cutoff > 100)
            {
                // Set error
                errorList.Add("Invalid cutoff. Must be 1-100.");
            }

            // Check error list 
            if (errorList.Count > 0)
            {
                // Print error(s)
                PrintErrors(errorList);

                // Halt flow
                return;
            }

            /* TODO Process lists [Can be DRYed, improved & put into a shared library] */

            // Advise
            Console.WriteLine("Processing...");

            /* Set MovieFile lists */

            // Set first list movie files
            List<MovieFile> firstList = new List<MovieFile>();

            // Set second list movies
            List<MovieFile> secondList = new List<MovieFile>();

            /* Set title cache dictionary */

            // Set first title cache dictionary
            Dictionary<string, string> firstTitleCacheDictionary = new Dictionary<string, string>();

            // Set second title cache dictionary
            Dictionary<string, string> secondTitleCacheDictionary = new Dictionary<string, string>();

            /* Set path cache dictionary */

            // Set first path cache dictionary
            Dictionary<string, string> firstPathCacheDictionary = new Dictionary<string, string>();

            // Set second path cache dictionary
            Dictionary<string, string> secondPathCacheDictionary = new Dictionary<string, string>();

            /* Populate with movies */

            // The regex file name pattern
            //#string regexFileNamePattern = "[^A-Za-z0-9_. ]+";
            string regexFileNamePattern = @"[^\u0000-\u007F]+"; // remove all non-ascii characters
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            // Set movie detector
            var detector = new MovieDetector();

            // Populate first list
            foreach (var tempMoviePath in File.ReadAllLines(options.firstList))
            {
                // Check for empty
                if (tempMoviePath.Length == 0)
                {
                    // Skip iteration
                    continue;
                }

                string moviePath = Regex.Replace(tempMoviePath.FoldToASCII().Trim(), regexFileNamePattern, string.Empty);

                foreach (char c in invalidChars)
                {
                    moviePath = moviePath.Replace(c.ToString(), string.Empty);
                }

                try
                {
                    // TODO Add movie file [Can fold and strip invalid characters by function]
                    firstList.Add(detector.GetInfo(moviePath));

                    // Add to first path cache dictionary
                    if (!firstPathCacheDictionary.ContainsKey(moviePath))
                    {
                        firstPathCacheDictionary.Add(moviePath, tempMoviePath);
                    }

                    // Add to first title cache dictionary
                    if (!firstTitleCacheDictionary.ContainsKey(moviePath))
                    {
                        firstTitleCacheDictionary.Add(moviePath, firstList[firstList.Count - 1].Title);
                    }
                }
                catch (Exception ex)
                {
                    //#
                    File.AppendAllText("Errors.txt", $"{Environment.NewLine}{tempMoviePath}");
                }
            }

            // Populate second list

            // Populate second list
            foreach (var tempMoviePath in File.ReadAllLines(options.secondList))
            {
                // Check for empty
                if (tempMoviePath.Length == 0)
                {
                    // Skip iteration
                    continue;
                }

                string moviePath = Regex.Replace(tempMoviePath.FoldToASCII().Trim(), regexFileNamePattern, string.Empty);

                foreach (char c in invalidChars)
                {
                    moviePath = moviePath.Replace(c.ToString(), string.Empty);
                }

                try
                {
                    // TODO Add movie file [Can fold and strip invalid characters by function]
                    secondList.Add(detector.GetInfo(moviePath));

                    // Add to second path cache dictionary
                    if (!secondPathCacheDictionary.ContainsKey(moviePath))
                    {
                        secondPathCacheDictionary.Add(moviePath, tempMoviePath);
                    }

                    // Add to second title cache dictionary
                    if (!secondTitleCacheDictionary.ContainsKey(moviePath))
                    {
                        secondTitleCacheDictionary.Add(moviePath, secondList[secondList.Count - 1].Title);
                    }
                }
                catch (Exception ex)
                {
                    //#
                    File.AppendAllText("Errors.txt", $"{Environment.NewLine}{tempMoviePath}");
                }
            }

            /** Pre-processing **/

            /* Prepended year */

            // TODO Remove prepended year from first list [DRY, function]
            for (int i = 0; i < firstList.Count; i++)
            {
                // Set title words list
                var titleWords = new List<string>(firstList[i].Title.Split(' '));

                // Check if there are 2+ words and first one is numeric
                if (titleWords.Count > 0 && int.TryParse(titleWords[0], out _))
                {
                    // Remove first word
                    titleWords.RemoveAt(0);

                    // Update title
                    firstList[i].Title = string.Join(" ", titleWords);
                }
            }

            // TODO Remove prepended year from second list [DRY, function]
            for (int i = 0; i < secondList.Count; i++)
            {
                // Set title words list
                var titleWords = new List<string>(secondList[i].Title.Split(' '));

                // Check if there are 2+ words and first one is numeric
                if (titleWords.Count > 0 && int.TryParse(titleWords[0], out _))
                {
                    // Remove second word
                    titleWords.RemoveAt(0);

                    // Update title
                    secondList[i].Title = string.Join(" ", titleWords);
                }
            }

            /* Lowercase all titles */

            // First list
            foreach (var item in firstList)
            {
                // Set lowercase 
                item.Title = item.Title.ToLowerInvariant();
            }

            // Second list
            foreach (var item in secondList)
            {
                // Set lowercase 
                item.Title = item.Title.ToLowerInvariant();
            }

            /* Remove function words */

            // Set the function words list
            List<string> functionWordsList = new List<string>() { "a", "an", "the", "this", "that", "these", "those", "my", "your", "their", "our", "some", "many", "few", "all", "and", "but", "or", "so", "because", "although", "in", "of", "on", "with", "by", "at", "over", "under", "he", "she", "it", "they", "we", "you", "me", "him", "her", "is", "am", "are", "was", "were", "has", "have", "had", "can", "could", "may", "might", "shall", "should", "will", "would", "must", "who", "what", "when", "where", "why", "how" };

            // Remove function words on first list
            for (int i = 0; i < firstList.Count; i++)
            {
                // Set title words list
                var titleWords = new List<string>(firstList[i].Title.Split(' '));

                // Check there are 2+ words 
                if (titleWords.Count < 2)
                {
                    // Skip iteration
                    continue;
                }

                // TODO Remove function words [Can improve logic]
                var tempTitleWords = titleWords.Except(functionWordsList);

                // Check there are words left
                if (tempTitleWords.Count() > 1)
                {
                    // Update title
                    firstList[i].Title = string.Join(" ", tempTitleWords);
                }
            }

            // Remove function words on second list
            for (int i = 0; i < secondList.Count; i++)
            {
                // Set title words list
                var titleWords = new List<string>(secondList[i].Title.Split(' '));

                // Check there are 2+ words 
                if (titleWords.Count < 2)
                {
                    // Skip iteration
                    continue;
                }

                // TODO Remove function words [Can improve logic]
                var tempTitleWords = titleWords.Except(functionWordsList);

                // Check there are words left
                if (tempTitleWords.Count() > 1)
                {
                    // Update title
                    secondList[i].Title = string.Join(" ", tempTitleWords);
                }
            }


            /** Add matches by direct title or fuzzy **/

            // The counter variables
            int matchesCount = 0, unmatchedCount = 0, collisionsCount = 0;

            // Strings for holding variables
            string matchesText = string.Empty, unmatchedText = string.Empty, collisionsText = string.Empty;

            // The matches (sorted) dictionary
            var matchesDictionary = new SortedDictionary<string, string>();

            // The collisions (sorted) dictionary
            var collisionsDictionary = new SortedDictionary<string, string>();

            // Check if by direct or fuzzy comparison (fuzzy-first)
            if (options.algorithm != "direct")
            {
                /* Set fuzzy matches collection */

                // Declare the default scorer
                IRatioScorer scorer = null;

                // TODO Set the scorer can improve logic
                switch (options.algorithm)
                {
                    // Default ratio
                    case "defaultratio":
                        scorer = ScorerCache.Get<DefaultRatioScorer>();
                        break;

                    // Partial ratio
                    case "partialratio":
                        scorer = ScorerCache.Get<PartialRatioScorer>();
                        break;

                    // Token set
                    case "tokenset":
                        scorer = ScorerCache.Get<TokenSetScorer>();
                        break;

                    // Partial token set
                    case "partialtokenset":
                        scorer = ScorerCache.Get<PartialTokenSetScorer>();
                        break;

                    // Token sort
                    case "tokensort":
                        scorer = ScorerCache.Get<TokenSortScorer>();
                        break;

                    // Partial token sort
                    case "partialtokensort":
                        scorer = ScorerCache.Get<PartialTokenSortScorer>();
                        break;

                    // Token abbreviation
                    case "tokenabbreviation":
                        scorer = ScorerCache.Get<TokenAbbreviationScorer>();
                        break;

                    // Partial token abbreviation
                    case "partialtokenabbreviation":
                        scorer = ScorerCache.Get<PartialTokenAbbreviationScorer>();
                        break;

                    // Weighted
                    case "weighted":
                        scorer = ScorerCache.Get<WeightedRatioScorer>();
                        break;
                }

                // Set cutoff
                int cutoff = options.cutoff;

                // Set first list titles
                List<string> firstListTitles = firstList.Select(x => x.Title).ToList<string>();

                // Set second list titles
                List<string> secondListTitles = secondList.Select(x => x.Title).ToList<string>();

                // The fuzzy matches (sorted) dictionary for the first list
                var fuzzyMatchesFirstListDictionary = new SortedDictionary<string, List<string>>();

                // The fuzzy matches (sorted) dictionary for the second list
                var fuzzyMatchesSecondListDictionary = new SortedDictionary<string, List<string>>();

                //# TODO Kludge [Look to remove it]
                var titleToPathDictionary = new Dictionary<string, string>();

                // Collect the fuzzy matches for the first list
                foreach (var item in firstList)
                {
                    // Get all fuzzy matches for the current item
                    var itemFuzzyMatches = FuzzySharp.Process.ExtractAll(item.Title, secondListTitles, null, scorer, cutoff);

                    // Iterate fuzzy matches
                    foreach (var fuzzyItem in itemFuzzyMatches)
                    {
                        // Check if must add
                        if (!fuzzyMatchesFirstListDictionary.ContainsKey(item.Title))
                        {
                            // Add into fuzzy matches first list dictionary
                            fuzzyMatchesFirstListDictionary.Add(item.Title, new List<string>());
                        }

                        // Populate with current match
                        fuzzyMatchesFirstListDictionary[item.Title].Add(secondList[fuzzyItem.Index].Path);
                    }
                }

                // Collect the fuzzy matches for the second list
                foreach (var item in secondList)
                {
                    // Get all fuzzy matches for the current item
                    var itemFuzzyMatches = FuzzySharp.Process.ExtractAll(item.Title, firstListTitles, null, scorer, cutoff);

                    // Iterate fuzzy matches
                    foreach (var fuzzyItem in itemFuzzyMatches)
                    {
                        // Check if must add
                        if (!fuzzyMatchesSecondListDictionary.ContainsKey(item.Title))
                        {
                            // Add into fuzzy matches second list dictionary
                            fuzzyMatchesSecondListDictionary.Add(item.Title, new List<string>());
                        }

                        // Populate with current match
                        fuzzyMatchesSecondListDictionary[item.Title].Add(firstList[fuzzyItem.Index].Path);
                    }
                }

                /* Add to matches dictionary */

                // Iterate first list dictionary keys
                foreach (var currentTitle in fuzzyMatchesFirstListDictionary.Keys)
                {
                    // Check if second list dictionary contains it
                    if (fuzzyMatchesSecondListDictionary.ContainsKey(currentTitle))
                    {
                        // Cached title
                        string cachedTitle = firstTitleCacheDictionary[fuzzyMatchesSecondListDictionary[currentTitle][0]];

                        // First list matches
                        var firstListMatches = new List<string>();

                        // Second list matches
                        var secondListMatches = new List<string>();

                        // Populate first list matches
                        foreach (var item in fuzzyMatchesSecondListDictionary[currentTitle].Distinct())
                        {
                            // Add by cached path
                            firstListMatches.Add(firstPathCacheDictionary[item]);
                        }

                        // Populate second list matches
                        foreach (var item in fuzzyMatchesFirstListDictionary[currentTitle].Distinct())
                        {
                            // Add by cached path
                            secondListMatches.Add(secondPathCacheDictionary[item]);
                        }

                        // Movie string
                        string movieString = $"{cachedTitle }{Environment.NewLine}First list:{Environment.NewLine}{string.Join(Environment.NewLine, firstListMatches)}{Environment.NewLine}Second list:{Environment.NewLine}{string.Join(Environment.NewLine, secondListMatches)}";

                        // Check it's unique
                        if (!matchesDictionary.ContainsKey(currentTitle))
                        {
                            // Add current movie to dictionary
                            matchesDictionary.Add(currentTitle, movieString);

                            // Check for collisions
                            if (fuzzyMatchesFirstListDictionary[currentTitle].Count() > 1 || fuzzyMatchesSecondListDictionary[currentTitle].Count() > 1)
                            {
                                // Add to collisions dictionary
                                collisionsDictionary.Add(currentTitle, movieString);
                            }
                        }
                    }
                }
            }
            else
            {
                /* Set matches collection */

                // Collect the matches
                var matchesCollection = firstList.Where(y => secondList.Any(z => z.Title == y.Title));

                // Populate matches dictionary
                foreach (var movieMatch in matchesCollection)
                {
                    // TODO 
                    string currentTitle = movieMatch.Title;

                    // Collect first list matches
                    var firstListMatches = firstList.Where(x => x.Title == currentTitle);

                    // Collect second list matches
                    var secondListMatches = secondList.Where(x => x.Title == currentTitle);

                    // Movie string
                    string movieString = $"{firstTitleCacheDictionary[movieMatch.Path]}{Environment.NewLine}First list:{Environment.NewLine}{string.Join(Environment.NewLine, firstListMatches.Select(f => firstPathCacheDictionary[f.Path]).ToList())}{Environment.NewLine}Second list:{Environment.NewLine}{string.Join(Environment.NewLine, secondListMatches.Select(s => secondPathCacheDictionary[s.Path]).ToList())}";

                    // Check it's unique
                    if (!matchesDictionary.ContainsKey(currentTitle))
                    {
                        // Add current movie to dictionary
                        matchesDictionary.Add(currentTitle, movieString);

                        // Check for collisions
                        if (firstListMatches.Count() > 1 || secondListMatches.Count() > 1)
                        {
                            // Add to collisions dictionary
                            collisionsDictionary.Add(movieMatch.Title, movieString);
                        }
                    }
                }
            }

            /* Set matches */

            // Check for matches
            if (matchesDictionary.Count > 0)
            {
                // Set into matches text 
                matchesText = string.Join($"{Environment.NewLine}{Environment.NewLine}", matchesDictionary.Values);

                // Update count
                matchesCount = matchesDictionary.Count;
            }

            /* Set unmatched */

            // Unmatched collection
            string unmatchedCollection = string.Empty;

            // First unmatched list
            var firstUnmatchedList = new List<string>();

            // TODO Compare against matched dictionary [Logic can be improved]
            foreach (var movie in firstList)
            {
                // Check for a match
                if (!matchesDictionary.ContainsKey(movie.Title.ToLowerInvariant()))
                {
                    // Add to unmatched list
                    firstUnmatchedList.Add(movie.Path);
                }
            }

            // Check there are unmatched items
            if (firstUnmatchedList.Count > 0)
            {
                // Sort unmatched list
                firstUnmatchedList.Sort();

                // Add to unmatched collection
                unmatchedCollection = $"First list:{Environment.NewLine}";
                unmatchedCollection += string.Join(Environment.NewLine, firstUnmatchedList);
            }

            // Second unmatched list
            var secondUnmatchedList = new List<string>();

            // TODO Compare against matched dictionary [Logic can be improved]
            foreach (var movie in secondList)
            {
                // Check for a match
                if (!matchesDictionary.ContainsKey(movie.Title.ToLowerInvariant()))
                {
                    // Add to unmatched list
                    secondUnmatchedList.Add(movie.Path);
                }
            }

            // Check there are unmatched items
            if (secondUnmatchedList.Count > 0)
            {
                // Sort unmatched list
                secondUnmatchedList.Sort();

                // Add to unmatched collection
                unmatchedCollection += $"{(firstUnmatchedList.Count > 0 ? $"{Environment.NewLine}{Environment.NewLine}" : string.Empty)}Second list:{Environment.NewLine}";
                unmatchedCollection += string.Join(Environment.NewLine, secondUnmatchedList);
            }

            // Set unmatched total
            int unmatchedTotal = firstUnmatchedList.Count + secondUnmatchedList.Count;

            // TODO Check there are unmatched items to display [Sum in variable, DRY]
            if (unmatchedTotal > 0)
            {
                // Set into text 
                unmatchedText = unmatchedCollection;

                // Update count
                unmatchedCount = unmatchedTotal;
            }

            /* Set collisions */

            // Check for collisions
            if (collisionsDictionary.Count > 0)
            {
                // Set into text box 
                collisionsText = string.Join($"{Environment.NewLine}{Environment.NewLine}", collisionsDictionary.Values);

                // Update count 
                collisionsCount = collisionsDictionary.Count;
            }

            /* Advise and save files */

            // Set directory in case it's "."
            if (options.directory == ".")
            {
                // Set to current assembly location
                options.directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }

            // Check for results
            if (matchesCount > 0 || unmatchedCount > 0 || collisionsCount > 0)
            {
                // Check if must create the directory
                if (!Directory.Exists(options.directory))
                {
                    // Create the directory
                    Directory.CreateDirectory(options.directory);
                }

                // Write results line
                Console.WriteLine($"{Environment.NewLine}RESULT(S):");

                // Check for matches
                if (matchesCount > 0)
                {
                    // Write matches
                    Console.WriteLine($"{Environment.NewLine}  {matchesCount} matches.");

                    // Set matches file path
                    string matchesPath = Path.Combine(options.directory, "matches.txt");

                    try
                    {
                        // Save to disk
                        File.WriteAllText(matchesPath, matchesText);
                    }
                    catch (Exception exception)
                    {
                        // Inform user
                        Console.WriteLine($"{Environment.NewLine}Error when saving to \"{matchesPath}\":{Environment.NewLine}{exception.Message}");
                    }
                }

                // Check for unmatched
                if (unmatchedCount > 0)
                {
                    // Write unmatched
                    Console.WriteLine($"{Environment.NewLine}  {unmatchedCount} unmatched.");

                    // Set unmatched file path
                    string unmatchedPath = Path.Combine(options.directory, "unmatched.txt");

                    try
                    {
                        // Save to disk
                        File.WriteAllText(unmatchedPath, unmatchedText);
                    }
                    catch (Exception exception)
                    {
                        // Inform user
                        Console.WriteLine($"{Environment.NewLine}Error when saving to \"{unmatchedPath}\":{Environment.NewLine}{exception.Message}");
                    }
                }

                // Check for collisions
                if (collisionsCount > 0)
                {
                    // Write collisions
                    Console.WriteLine($"{Environment.NewLine}  {collisionsCount} collisions.");

                    // Set collisions file path
                    string collisionsPath = Path.Combine(options.directory, "collisions.txt");

                    try
                    {
                        // Save to disk
                        File.WriteAllText(collisionsPath, collisionsText);
                    }
                    catch (Exception exception)
                    {
                        // Inform user
                        Console.WriteLine($"{Environment.NewLine}Error when saving to \"{collisionsPath}\":{Environment.NewLine}{exception.Message}");
                    }
                }
            }
            else
            {
                // Write no results line
                Console.WriteLine($"{Environment.NewLine}No matches, unmatched or collisions to save.");
            }
        }

        /// <summary>
        /// Prints the errors.
        /// </summary>
        /// <param name="errorList">Error list.</param>
        static void PrintErrors(List<string> errorList)
        {
            // Print errors
            Console.WriteLine($"{Environment.NewLine}ERROR(S):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", errorList)}");
        }

        /// <summary>
        /// Handles the parse error.
        /// </summary>
        /// <param name="errors">Errors.</param>
        static void HandleParseError(IEnumerable<Error> errors)
        {
            // Extended error messages can be processed here
        }
    }
}