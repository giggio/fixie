﻿namespace Fixie.Execution.Listeners
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Report
    {
        readonly List<ClassReport> classes;

        public Report(string location)
        {
            classes = new List<ClassReport>();
            Location = location;
        }

        public void Add(ClassReport classReport) => classes.Add(classReport);

        public string Location { get; }

        public TimeSpan Duration => new TimeSpan(classes.Sum(result => result.Duration.Ticks));

        public IReadOnlyList<ClassReport> Classes => classes;

        public int Passed => classes.Sum(@class => @class.Passed);
        public int Failed => classes.Sum(@class => @class.Failed);
        public int Skipped => classes.Sum(@class => @class.Skipped);
        public int Total => Passed + Failed + Skipped;
    }
}