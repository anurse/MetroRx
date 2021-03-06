﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Text;
using GalaSoft.MvvmLight;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MetroRx;
using Windows.UI.Xaml.Data;

namespace MetroRx.Tests
{
    public class HostTestFixture : ViewModelBase
    {
        string _SomeParam;
        public string SomeParam {
            get { return _SomeParam; }
            set { _SomeParam = value; this.RaisePropertyChanged(() => this.SomeParam); }
        }

        int _SomeOtherParam;
        public int SomeOtherParam {
            get { return _SomeOtherParam; }
            set { _SomeOtherParam = value; this.RaisePropertyChanged(() => this.SomeOtherParam); }
        }
    }

    [TestClass]
    public class NotifyPropertyChangedMixinTest
    {
        [TestMethod]
        public void RaisePropertyChangedTest()
        {
            var fixture = new HostTestFixture();
            string propChanged = null;

            fixture.PropertyChanged += (o, e) => propChanged = e.PropertyName;

            fixture.SomeParam = "Foo";
            Assert.AreEqual("SomeParam", propChanged);

            fixture.SomeOtherParam = 4;
            Assert.AreEqual("SomeOtherParam", propChanged);
        }

        [TestMethod]
        public void ChangedObservableTest()
        {
            var input = new[] { 1, 2, 3 };
            var fixture = new HostTestFixture();

            IList<IObservedChange<HostTestFixture, object>> result = null;

            fixture.Changed()
                .Take(input.Length)
                .ToList()
                .Subscribe(x => result = x);

            foreach (var v in input) { fixture.SomeOtherParam = v; }

            Assert.IsNotNull(result);
            Assert.AreEqual(input.Length, result.Count);
            foreach (var v in input.Zip(result.Select(x => (int)x.Value), (expected, actual) => new { expected, actual })) {
                Assert.AreEqual(v.expected, v.actual);
            }
        }

        [TestMethod]
        public void FromPropertyTest()
        {
            var input = new[] { 1, 2, 3 };
            var fixture = new HostTestFixture();

            IList<IObservedChange<HostTestFixture, int>> result = null;
            fixture.FromProperty(x => x.SomeOtherParam)
                .Take(input.Length)
                .ToList()
                .Subscribe(x => result = x);

            foreach (var v in input) { fixture.SomeOtherParam = v; }

            Assert.IsNotNull(result);
            Assert.AreEqual(input.Length, result.Count);
            foreach (var v in input.Zip(result.Select(x => x.Value), (expected, actual) => new { expected, actual })) {
                Assert.AreEqual(v.expected, v.actual);
            }
        }
    }

#if FALSE
    public class HostTestFixture : ReactiveObject
    {
        public TestFixture _Child;
        public TestFixture Child {
            get { return _Child; }
            set { this.RaiseAndSetIfChanged(x => x.Child, value); }
        }

        public int _SomeOtherParam;
        public int SomeOtherParam {
            get { return _SomeOtherParam; }
            set { this.RaiseAndSetIfChanged(x => x.SomeOtherParam, value); }
        }

        public NonObservableTestFixture _PocoChild;
        public NonObservableTestFixture PocoChild {
            get { return _PocoChild; }
            set { this.RaiseAndSetIfChanged(x => x.PocoChild, value); }
        }
    }

    public class NonObservableTestFixture
    {
        public TestFixture Child {get; set;}
    }

    public class NonReactiveINPCObject : INotifyPropertyChanged
    {
        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        TestFixture _InpcProperty;
        public TestFixture InpcProperty {
            get { return _InpcProperty; }
            set {
                if (_InpcProperty == value) {
                    return;
                }
                _InpcProperty = value;

                PropertyChanged(this, new PropertyChangedEventArgs("InpcProperty"));
            }
        }
    }

    public class ObjChain1 : ReactiveObject
    {
        ObjChain2 _Model = new ObjChain2();
        public ObjChain2 Model {
            get { return _Model; }
            set { this.RaiseAndSetIfChanged(x => x.Model, value); }
        }
    }

    public class ObjChain2 : ReactiveObject
    {
        ObjChain3 _Model = new ObjChain3();
        public ObjChain3 Model {
            get { return _Model; }
            set { this.RaiseAndSetIfChanged(x => x.Model, value); }
        }
    }

    public class ObjChain3 : ReactiveObject
    {
        HostTestFixture _Model = new HostTestFixture();
        public HostTestFixture Model {
            get { return _Model; }
            set { this.RaiseAndSetIfChanged(x => x.Model, value); }
        }
    }

    public class ReactiveNotifyPropertyChangedMixinTest : IEnableLogger
    {
        [Fact]
        public void OFPSimplePropertyTest()
        {
            (new TestScheduler()).With(sched => {
                var fixture = new TestFixture();
                var changes = fixture.ObservableForProperty(x => x.IsOnlyOneWord).CreateCollection();

                fixture.IsOnlyOneWord = "Foo";
                sched.Start();
                Assert.Equal(1, changes.Count);

                fixture.IsOnlyOneWord = "Bar";
                sched.Start();
                Assert.Equal(2, changes.Count);

                fixture.IsOnlyOneWord = "Baz";
                sched.Start();
                Assert.Equal(3, changes.Count);

                fixture.IsOnlyOneWord = "Baz";
                sched.Start();
                Assert.Equal(3, changes.Count);

                Assert.True(changes.All(x => x.Sender == fixture));
                Assert.True(changes.All(x => x.PropertyName == "IsOnlyOneWord"));
                changes.Select(x => x.Value).AssertAreEqual(new[] {"Foo", "Bar", "Baz"});
            });
        }

        [Fact]
        public void OFPSimpleChildPropertyTest()
        {
            (new TestScheduler()).With(sched => {
                var fixture = new HostTestFixture() {Child = new TestFixture()};
                var changes = fixture.ObservableForProperty(x => x.Child.IsOnlyOneWord).CreateCollection();

                fixture.Child.IsOnlyOneWord = "Foo";
                sched.Start();
                Assert.Equal(1, changes.Count);

                fixture.Child.IsOnlyOneWord = "Bar";
                sched.Start();
                Assert.Equal(2, changes.Count);

                fixture.Child.IsOnlyOneWord = "Baz";
                sched.Start();
                Assert.Equal(3, changes.Count);

                fixture.Child.IsOnlyOneWord = "Baz";
                sched.Start();
                Assert.Equal(3, changes.Count);

                Assert.True(changes.All(x => x.Sender == fixture));
                Assert.True(changes.All(x => x.PropertyName == "Child.IsOnlyOneWord"));
                changes.Select(x => x.Value).AssertAreEqual(new[] {"Foo", "Bar", "Baz"});
            });
        }

        [Fact]
        public void OFPReplacingTheHostShouldResubscribeTheObservable()
        {
            (new TestScheduler()).With(sched => {
                var fixture = new HostTestFixture() {Child = new TestFixture()};
                var changes = fixture.ObservableForProperty(x => x.Child.IsOnlyOneWord).CreateCollection();

                fixture.Child.IsOnlyOneWord = "Foo";
                sched.Start();
                Assert.Equal(1, changes.Count);

                fixture.Child.IsOnlyOneWord = "Bar";
                sched.Start();
                Assert.Equal(2, changes.Count);

                // Tricky! This is a change too, because from the perspective 
                // of the binding, we've went from "Bar" to null
                fixture.Child = new TestFixture();
                sched.Start();
                Assert.Equal(3, changes.Count);

                // Here we've set the value but it shouldn't change
                fixture.Child.IsOnlyOneWord = null;
                sched.Start();
                Assert.Equal(3, changes.Count);

                fixture.Child.IsOnlyOneWord = "Baz";
                sched.Start();
                Assert.Equal(4, changes.Count);

                fixture.Child.IsOnlyOneWord = "Baz";
                sched.Start();
                Assert.Equal(4, changes.Count);

                Assert.True(changes.All(x => x.Sender == fixture));
                Assert.True(changes.All(x => x.PropertyName == "Child.IsOnlyOneWord"));
                changes.Select(x => x.Value).AssertAreEqual(new[] {"Foo", "Bar", null, "Baz"});
            });           
        }


        [Fact]
        public void OFPReplacingTheHostWithNullThenSettingItBackShouldResubscribeTheObservable()
        {
            (new TestScheduler()).With(sched => {
                var fixture = new HostTestFixture() {Child = new TestFixture()};
                var changes = fixture.ObservableForProperty(x => x.Child.IsOnlyOneWord).CreateCollection();

                fixture.Child.IsOnlyOneWord = "Foo";
                sched.Start();
                Assert.Equal(1, changes.Count);

                fixture.Child.IsOnlyOneWord = "Bar";
                sched.Start();
                Assert.Equal(2, changes.Count);

                // Oops, now the child is Null, we may now blow up
                fixture.Child = null;
                sched.Start();
                Assert.Equal(2, changes.Count);

                // Tricky! This is a change too, because from the perspective 
                // of the binding, we've went from "Bar" to null
                fixture.Child = new TestFixture();
                sched.Start();
                Assert.Equal(3, changes.Count);

                Assert.True(changes.All(x => x.Sender == fixture));
                Assert.True(changes.All(x => x.PropertyName == "Child.IsOnlyOneWord"));
                changes.Select(x => x.Value).AssertAreEqual(new[] {"Foo", "Bar", null});
            });
        }

        [Fact]
        public void OFPChangingTheHostPropertyShouldFireAChildChangeNotificationOnlyIfThePreviousChildIsDifferent()
        {
            (new TestScheduler()).With(sched => {
                var fixture = new HostTestFixture() {Child = new TestFixture()};
                var changes = fixture.ObservableForProperty(x => x.Child.IsOnlyOneWord).CreateCollection();

                fixture.Child.IsOnlyOneWord = "Foo";
                sched.Start();
                Assert.Equal(1, changes.Count);

                fixture.Child.IsOnlyOneWord = "Bar";
                sched.Start();
                Assert.Equal(2, changes.Count);

                fixture.Child = new TestFixture() {IsOnlyOneWord = "Bar"};
                sched.Start();
                Assert.Equal(2, changes.Count);
            });
        }

        [Fact]
        public void OFPShouldWorkWithINPCObjectsToo()
        {
            (new TestScheduler()).With(sched => {
                var fixture = new NonReactiveINPCObject() { InpcProperty = null };

                var changes = fixture.ObservableForProperty(x => x.InpcProperty.IsOnlyOneWord).CreateCollection();

                fixture.InpcProperty = new TestFixture();
                sched.Start();
                Assert.Equal(1, changes.Count);

                fixture.InpcProperty.IsOnlyOneWord = "Foo";
                sched.Start();
                Assert.Equal(2, changes.Count);

                fixture.InpcProperty.IsOnlyOneWord = "Bar";
                sched.Start();
                Assert.Equal(3, changes.Count);

                fixture.InpcProperty = new TestFixture() {IsOnlyOneWord = "Bar"};
                sched.Start();
                Assert.Equal(4, changes.Count);
            });
        }

        [Fact]
        public void AnyChangeInExpressionListTriggersUpdate() 
        {
            var obj = new ObjChain1();
            bool obsUpdated;
            obj.ObservableForProperty(x => x.Model.Model.Model.SomeOtherParam).Subscribe(_ => obsUpdated = true);
           
            obsUpdated = false;
            obj.Model.Model.Model.SomeOtherParam = 42;
            Assert.True(obsUpdated);
 
            obsUpdated = false;
            obj.Model.Model.Model = new HostTestFixture();
            Assert.True(obsUpdated);
 
            obsUpdated = false;
            obj.Model.Model = new ObjChain3() {Model = new HostTestFixture() {SomeOtherParam = 10 } } ;
            Assert.True(obsUpdated);
 
            obsUpdated = false;
            obj.Model = new ObjChain2();
            Assert.True(obsUpdated);
        }

        [Fact]
        public void SubscriptionToWhenAnyShouldReturnCurrentValue()
        {
            var obj = new HostTestFixture();
            int observedValue = 1;
            obj.WhenAny(x => x.SomeOtherParam, x => x.Value)
               .Subscribe(x => observedValue = x);

            obj.SomeOtherParam = 42;
            
            Assert.True(observedValue == obj.SomeOtherParam);
        }

        [Fact]
        public void MultiPropertyExpressionsShouldBeProperlyResolved()
        {
            var data = new Dictionary<Expression<Func<HostTestFixture, object>>, string[]>() {
                {x => x.SomeOtherParam, new[] {"SomeOtherParam"}},
                {x => x.Child.IsNotNullString, new[] {"Child", "IsNotNullString"}},
                {x => x.Child.Changed, new[] {"Child", "Changed"}},
                {x => x.Child.IsOnlyOneWord.Length, new[] {"Child", "IsOnlyOneWord", "Length"}},
            };

            var results = data.Keys.Select(x => new {input = x, output = RxApp.expressionToPropertyNames(x)});

            foreach(var x in results) {
                this.Log().InfoFormat("Attempted {0}, expected [{1}]", x.input, String.Join(",", data[x.input]));
                data[x.input].AssertAreEqual(x.output);
            }
        }

        [Fact]
        public void WhenAnySmokeTest()
        {
            (new TestScheduler()).With(sched => {
                var fixture = new HostTestFixture() {Child = new TestFixture()};
                fixture.SomeOtherParam = 5;
                fixture.Child.IsNotNullString = "Foo";

                var output1 = new List<IObservedChange<HostTestFixture, int>>();
                var output2 = new List<IObservedChange<HostTestFixture, string>>();
                fixture.WhenAny(x => x.SomeOtherParam, x => x.Child.IsNotNullString, (sop, nns) => new {sop, nns}).Subscribe(x => {
                    output1.Add(x.sop); output2.Add(x.nns);
                });

                sched.Start();
                Assert.Equal(1, output1.Count);
                Assert.Equal(1, output2.Count);
                Assert.Equal(fixture, output1[0].Sender);
                Assert.Equal(fixture, output2[0].Sender);
                Assert.Equal(5, output1[0].Value);
                Assert.Equal("Foo", output2[0].Value);

                fixture.SomeOtherParam = 10;
                sched.Start();
                Assert.Equal(2, output1.Count);
                Assert.Equal(2, output2.Count);
                Assert.Equal(fixture, output1[1].Sender);
                Assert.Equal(fixture, output2[1].Sender);
                Assert.Equal(10, output1[1].Value);
                Assert.Equal("Foo", output2[1].Value);

                fixture.Child.IsNotNullString = "Bar";
                sched.Start();
                Assert.Equal(3, output1.Count);
                Assert.Equal(3, output2.Count);
                Assert.Equal(fixture, output1[2].Sender);
                Assert.Equal(fixture, output2[2].Sender);
                Assert.Equal(10, output1[2].Value);
                Assert.Equal("Bar", output2[2].Value);
            });
        }

        [Fact]
        public void WhenAnyShouldWorkEvenWithNormalProperties()
        {
            var fixture = new TestFixture() { IsNotNullString = "Foo", IsOnlyOneWord = "Baz", PocoProperty = "Bamf" };

            var output = new List<IObservedChange<TestFixture, string>>();
            fixture.WhenAny(x => x.PocoProperty, x => x).Subscribe(output.Add);

            Assert.Equal(1, output.Count);
            Assert.Equal(fixture, output[0].Sender);
            Assert.Equal("PocoProperty", output[0].PropertyName);
            Assert.Equal("Bamf", output[0].Value);
        }
    }
#endif
}