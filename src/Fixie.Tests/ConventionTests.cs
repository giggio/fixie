﻿using System;

namespace Fixie.Tests
{
    public class ConventionTests
    {
        public void ShouldExecuteAllCasesInAllDiscoveredFixtures()
        {
            var listener = new StubListener();
            var convention = new SelfTestConvention();

            convention.Execute(listener, typeof(SampleIrrelevantClass), typeof(PassFixture), typeof(int), typeof(PassFailFixture));

            listener.ShouldHaveEntries("Fixie.Tests.ConventionTests+PassFailFixture.Pass passed.",
                                       "Fixie.Tests.ConventionTests+PassFailFixture.Fail failed: Exception of type 'System.Exception' was thrown.",
                                       "Fixie.Tests.ConventionTests+PassFixture.PassA passed.",
                                       "Fixie.Tests.ConventionTests+PassFixture.PassB passed.");
        }

        class SampleIrrelevantClass
        {
            public void PassA() { }
            public void PassB() { }
        }

        class PassFixture
        {
            public void PassA() { }
            public void PassB() { }
        }

        class PassFailFixture
        {
            public void Pass() { }
            public void Fail() { throw new Exception(); }
        }
    }
}