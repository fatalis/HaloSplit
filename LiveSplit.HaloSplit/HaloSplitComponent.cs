using System.Linq;
using LiveSplit.HaloSplit.UI;
using LiveSplit.Model;
using LiveSplit.UI.Components;
using LiveSplit.UI;
using System;
using System.Xml;
using System.Windows.Forms;

namespace LiveSplit.HaloSplit
{
    class HaloSplitComponent : LogicComponent
    {
        public override string ComponentName
        {
            get { return "HaloSplit"; }
        }

        private HaloSplitUIComponent UI
        {
            get {
                return _state.Layout.Components.FirstOrDefault(
                    c => c.GetType() == typeof(HaloSplitUIComponent)) as HaloSplitUIComponent;
            }
        }

        private TimerModel _timer;
        private LiveSplitState _state;
        private GameMemory _gameMemory;
        private DateTime? _splitTime;

        public HaloSplitComponent(LiveSplitState state)
        {
            _state = state;

            _timer = new TimerModel();
            _timer.CurrentState = state;

            _gameMemory = new GameMemory();
            // possible thread safety issues in all of these event handlers
            _gameMemory.OnMapChanged += gameMemory_OnMapChanged;
            _gameMemory.OnGainControl += gameMemory_OnGainControl;
            _gameMemory.OnLostControl += gameMemory_OnLostControl;
            _gameMemory.OnReset += gameMemory_OnReset;
            _gameMemory.OnPlayerDeath += gameMemory_OnPlayerDeath;
            _gameMemory.StartReading();
        }

        public override void Dispose()
        {
            if (_gameMemory != null)
                _gameMemory.Stop();
        }

        void gameMemory_OnMapChanged(object sender, string map)
        {
            if (map != @"levels\a10\a10")
            {
                _splitTime = DateTime.Now;
                _timer.Split();
            }
        }

        void gameMemory_OnReset(object sender, EventArgs e)
        {
            _timer.Reset();
        }

        void gameMemory_OnGainControl(object sender, EventArgs e)
        {
            _timer.Start();
        }

        void gameMemory_OnLostControl(object sender, EventArgs eventArgs)
        {
            // hacky fix for double split issue on The Maw
            if (_splitTime.HasValue && DateTime.Now - _splitTime.Value > TimeSpan.FromSeconds(10))
                _timer.Split();
        }

        void gameMemory_OnPlayerDeath(object sender, EventArgs e)
        {
            if (_state.CurrentPhase != TimerPhase.NotRunning && _state.CurrentPhase != TimerPhase.Ended)
            {
                if (this.UI != null)
                    this.UI.AddDeath();
            }
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) { }
        public override XmlNode GetSettings(XmlDocument document) { return document.CreateElement("Settings"); }
        public override Control GetSettingsControl(LayoutMode mode) { return null; }
        public override void SetSettings(XmlNode settings) { }
        public override void RenameComparison(string oldName, string newName) { }
    }
}
