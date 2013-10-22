using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Controls.DomainServices.Test
{
    /// <summary>
    /// Mock implementation of the <see cref="DomainDataSource.ITimer"/> interface. This mock
    /// will record the way this type is used by the <see cref="DomainDataSource"/>. Also, this
    /// mock allows <see cref="Tick"/> events to be raised programatically eliminating the need
    /// for delay-based testing.
    /// </summary>
    public abstract class MockTimer : DomainDataSource.ITimer
    {
        private bool _isEnabled;
        private TimeSpan _interval;
        private EventHandler _handler;
        private IList<TimeSpan> _intervalList = new List<TimeSpan>();
        private IList<TimeSpan> _expectedIntervalList = new List<TimeSpan>();

        #region DomainDataSource.ITimer

        bool DomainDataSource.ITimer.IsEnabled
        {
            get { return this._isEnabled; }
        }

        TimeSpan DomainDataSource.ITimer.Interval
        {
            get
            {
                return this._interval;
            }
            set
            {
                this._interval = value;
                this._intervalList.Add(value);
            }
        }

        event EventHandler DomainDataSource.ITimer.Tick
        {
            add
            {
                this._handler += value;
                this.TickAddCount++;
            }

            remove
            {
                this._handler -= value;
                this.TickRemoveCount++;
            }
        }

        void DomainDataSource.ITimer.Start()
        {
            this._isEnabled = true;
            this.StartCount++;
        }

        void DomainDataSource.ITimer.Stop()
        {
            this._isEnabled = false;
            this.StopCount++;
        }

        #endregion

        // Actual counts
        public IList<TimeSpan> IntervalList { get { return this._intervalList; } }
        public int StartCount { get; set; }
        public int StopCount { get; set; }
        public int TickAddCount { get; set; }
        public int TickRemoveCount { get; set; }

        // Expected counts
        public IList<TimeSpan> ExpectedIntervalList { get { return this._expectedIntervalList; } }
        public int ExpectedStartCount { get; set; }
        public int ExpectedStopCount { get; set; }
        public int ExpectedTickAddCount { get; set; }
        public int ExpectedTickRemoveCount{ get; set; }

        /// <summary>
        /// Raises a <see cref="Tick"/> event.
        /// </summary>
        public void RaiseTick()
        {
            EventHandler handler = this._handler;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public void SetExpectedInterval(TimeSpan interval)
        {
            this.ExpectedIntervalList.Clear();
            this.ExpectedIntervalList.Add(interval);
        }
    }

    public class LoadTimer : MockTimer
    {
        private DomainDataSource _dds;

        internal void UseWithDDS(DomainDataSource dds)
        {
            this.UseWithDDS(dds, "when setting the LoadTimer.");
        }

        internal void UseWithDDS(DomainDataSource dds, string message)
        {
            this._dds = dds;

            TimerHelper.AssertInitialization(this, () => dds.LoadTimer = this, dds.LoadDelay, message);
        }

        public void AssertStart(Action startAction)
        {
            this.AssertStart(startAction, "when starting the LoadTimer.");
        }

        public void AssertStart(Action startAction, string message)
        {
            TimerHelper.AssertStart(this, startAction, this._dds.LoadDelay, message);
        }

        public void AssertLoadingDataOnTick()
        {
            this.AssertLoadingDataOnTick("when raising the LoadTimer.Tick event.");
        }

        public void AssertLoadingDataOnTick(string message)
        {
            this.ExpectedStopCount = 1;
            TimerHelper.AssertLoadingDataOnTick(this, this._dds, message);
        }
    }

    public class ProgressiveLoadTimer : MockTimer
    {
        private DomainDataSource _dds;

        internal void UseWithDDS(DomainDataSource dds)
        {
            this.UseWithDDS(dds, "when setting the ProgressiveLoadTimer.");
        }

        internal void UseWithDDS(DomainDataSource dds, string message)
        {
            this._dds = dds;

            TimerHelper.AssertInitialization(this, () => dds.ProgressiveLoadTimer = this, dds.LoadInterval, message);
        }

        public void AssertLoadingDataOnTick()
        {
            this.AssertLoadingDataOnTick("when raising the ProgressiveLoadTimer.Tick event.");
        }

        public void AssertLoadingDataOnTick(string message)
        {
            this.ExpectedStopCount = 1;
            TimerHelper.AssertLoadingDataOnTick(this, this._dds, message);
        }
    }

    public class RefreshLoadTimer : MockTimer
    {
        private DomainDataSource _dds;

        internal void UseWithDDS(DomainDataSource dds)
        {
            this.UseWithDDS(dds, "when setting the RefreshLoadTimer.");
        }

        internal void UseWithDDS(DomainDataSource dds, string message)
        {
            this._dds = dds;

            TimerHelper.AssertInitialization(this, () => dds.RefreshLoadTimer = this, dds.RefreshInterval, message);
        }

        public void AssertLoadingDataOnTick()
        {
            this.AssertLoadingDataOnTick("when raising the RefreshLoadTimer.Tick event.");
        }

        public void AssertLoadingDataOnTick(string message)
        {
            TimerHelper.AssertLoadingDataOnTick(this, this._dds, message);
        }

        public void AssertStarted()
        {
            this.AssertStarted("when setting the RefreshInterval.");
        }

        public void AssertStarted(string message)
        {
            TimerHelper.AssertStart(this, () => {}, this._dds.RefreshInterval, message);
        }
    }

    public static class TimerHelper
    {
        public static void ResetActualCounts(MockTimer timer)
        {
            timer.IntervalList.Clear();
            timer.StartCount = 0;
            timer.StopCount = 0;
            timer.TickAddCount = 0;
            timer.TickRemoveCount = 0;
        }

        public static void ResetExpectedCounts(MockTimer timer)
        {
            timer.ExpectedIntervalList.Clear();
            timer.ExpectedStartCount = 0;
            timer.ExpectedStopCount = 0;
            timer.ExpectedTickAddCount = 0;
            timer.ExpectedTickRemoveCount = 0;
        }

        public static void ResetCounts(MockTimer timer)
        {
            TimerHelper.ResetActualCounts(timer);
            TimerHelper.ResetExpectedCounts(timer);
        }

        public static void AssertCounts(MockTimer timer, string message)
        {
            if (timer.ExpectedIntervalList == null)
            {
                Assert.AreEqual(0, timer.IntervalList.Count,
                    "The interval list should not contain any time spans " + message);
            }
            else if (timer.ExpectedIntervalList.Count == 1)
            {
                Assert.AreEqual(timer.ExpectedIntervalList[0], ((DomainDataSource.ITimer)timer).Interval,
                    "The interval should match the expected interval " + message);
            }
            else
            {
                string intervalListText = string.Join(",", timer.IntervalList.ToArray());
                string expectedIntervalListText = string.Join(",", timer.ExpectedIntervalList.ToArray());
                Assert.IsTrue(timer.ExpectedIntervalList.SequenceEqual(timer.IntervalList),
                    "The interval list should match the expected interval list " + message +
                    " Expected=" + expectedIntervalListText +
                    ", Actual=" + intervalListText);
            }

            Assert.AreEqual(timer.ExpectedStartCount, timer.StartCount,
                "The Start counts should be equal " + message);
            Assert.AreEqual(timer.ExpectedStopCount, timer.StopCount,
                "The Stop counts should be equal " + message);
            Assert.AreEqual(timer.ExpectedTickAddCount, timer.TickAddCount,
                "The Tick add counts should be equal " + message);
            Assert.AreEqual(timer.ExpectedTickRemoveCount, timer.TickRemoveCount,
                "The Tick remove counts should be equal " + message);
        }

        public static void AssertAndResetCounts(this MockTimer timer, string message)
        {
            TimerHelper.AssertCounts(timer, message);
            TimerHelper.ResetCounts(timer);
        }

        public static void AssertInitialization(MockTimer timer, Action initializationAction, TimeSpan interval, string message)
        {
            timer.SetExpectedInterval(interval);
            timer.ExpectedTickAddCount = 1;

            initializationAction();

            TimerHelper.AssertAndResetCounts(timer, message);
        }

        public static void AssertStart(MockTimer timer, Action startAction, TimeSpan interval, string message)
        {
            timer.SetExpectedInterval(interval);
            timer.ExpectedStartCount = 1;

            startAction();

            TimerHelper.AssertAndResetCounts(timer, message);
        }

        public static void AssertLoadingDataOnTick(MockTimer timer, DomainDataSource dds, string message)
        {
            // Add LoadingData handler
            int loadingDataCount = 0;
            EventHandler<LoadingDataEventArgs> handler = (sender, e) => loadingDataCount++;
            dds.LoadingData += handler;

            timer.RaiseTick();

            // Remove LoadingData handler
            dds.LoadingData -= handler;

            Assert.AreEqual(1, loadingDataCount,
                "A LoadingData event should have been raised " + message);
            TimerHelper.AssertAndResetCounts(timer, message);
        }
    }
}
