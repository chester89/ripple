using System;
using System.ComponentModel;
using FubuCore;
using FubuCore.CommandLine;
using ripple.Local;
using ripple.Model;

namespace ripple.Commands
{
    public class LocalInput
    {
        [Description("Optionally specifies a solution to start from")]
        [FlagAlias("from", 'f')]
        public string FromFlag { get; set; }
        
        [Description("Optionally specifies a solution to stop at")]
        public string ToFlag { get; set; }

        [Description("If set, bypasses all intermediate solutions and moves build products directly")]
        public bool DirectFlag { get; set; }

        [Description("Use the 'fast' build option for solutions, i.e. compile but don't run any pesky unit test kinda things")]
        [FlagAlias("fast", 'x')]
        public bool FastFlag { get; set; }

		[Description("Use the 'no build' option to just shuffle binaries around (i.e., don't compile after moving)")]
		[FlagAlias("nobuild", 'n')]
		public bool NoBuildFlag { get; set; }

        [Description("Writes out all the build output to the screen")]
        public bool VerboseFlag { get; set; }

        public RipplePlanRequirements ToRequirements()
        {
            return new RipplePlanRequirements(){
                Direct = DirectFlag,
                Fast = FastFlag,
                From = FromFlag,
                To = ToFlag,
                Verbose = VerboseFlag,
				SkipBuild = NoBuildFlag
            };
        }
    }

    [CommandDescription("Performs the local ripple")]
    public class LocalCommand : FubuCommand<LocalInput>
    {
        public override bool Execute(LocalInput input)
        {
            new WhereAmICommand().Execute(new WhereAmIInput());
            Console.WriteLine();

            var requirements = input.ToRequirements();
            var solutionGraph = SolutionGraphBuilder.BuildForCurrentDirectory();

            var logger = new RippleLogger();
            var stepRunner = new Local.RippleStepRunner(new ProcessRunner(), new FileSystem(), logger, requirements);
            var runner = new RippleRunner(logger, stepRunner);

            runner.RunPlan(solutionGraph, requirements);

            return true;
        }
    }
}