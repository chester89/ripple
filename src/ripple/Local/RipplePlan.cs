using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ripple.Model;

namespace ripple.Local
{
    public class RipplePlan : IEnumerable<IRippleStep>
    {
        private readonly IList<IRippleStep> _steps = new List<IRippleStep>();

        public RipplePlan(IEnumerable<Solution> solutions, bool skipBuild)
        {
            guardCondition(solutions);

            var queue = new Queue<Solution>(solutions); 

            // first step
            _steps.Add(new BuildSolution(queue.Dequeue()));

            while (queue.Any())
            {
                var solution = queue.Dequeue();

                var nugets = solution
                    .NugetDependencies
                    .Where(x => solutions.Contains(x.Publisher))
                    .OrderBy(x => x.Name)
                    .Select(x => new MoveNugetAssemblies(x, solution));

                _steps.AddRange(nugets);

				if (!skipBuild)
				{
					_steps.Add(new BuildSolution(solution));
				}
            }

        }

        public int Count
        {
            get
            {
                return _steps.Count;
            }
        }

        private static void guardCondition(IEnumerable<Solution> solutions)
        {
            if (solutions.Count() < 2)
            {
                throw new InvalidOperationException("Cannot execute a ripple with less than 2 solutions.  It's just plain silly");
            }
        }

        public void Execute(IRippleStepRunner runner)
        {
            _steps.Each(x => x.Execute(runner));
        }

        public IEnumerator<IRippleStep> GetEnumerator()
        {
            return _steps.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}