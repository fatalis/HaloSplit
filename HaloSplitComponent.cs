using LiveSplit.Model;
using LiveSplit.UI.Components;
using LiveSplit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.Windows.Forms;

namespace LiveSplit.HaloSplit
{
    class HaloSplitComponent : IComponent
    {
        public string ComponentName
        {
            get { return "HaloSplit"; }
        }

        public IDictionary<string, Action> ContextMenuControls { get; protected set; }

        private TimerModel _timer;
        private GameMemory _gameMemory;
        private DateTime? _splitTime;

        public HaloSplitComponent(LiveSplitState state)
        {
            this.ContextMenuControls = new Dictionary<String, Action>();

            _timer = new TimerModel();
            _timer.CurrentState = state;

            _gameMemory = new GameMemory();
            // possible thread safety issues in all of these event handlers
            // invoking on main thread could lose a few frames of timer accuracy
            _gameMemory.OnMapChanged += gameMemory_OnMapChanged;
            _gameMemory.OnGainControl += gameMemory_OnGainControl;
            _gameMemory.OnLostControl += gameMemory_OnLostControl;
            _gameMemory.OnReset += gameMemory_OnReset;
            _gameMemory.StartReading();
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

        ~HaloSplitComponent()
        {
            // TODO: in LiveSplit 1.4, components will be IDisposable
            //_gameMemory.Stop();
        }

        public XmlNode GetSettings(XmlDocument document) { return document.CreateElement("Settings"); }
        public Control GetSettingsControl(LayoutMode mode) { return null; }
        public void SetSettings(XmlNode settings) { }
        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) { }
        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region region) { }
        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region region) { }
        public void RenameComparison(string oldName, string newName) { }
        public float VerticalHeight { get { return 0; } }
        public float MinimumWidth { get { return 0; } }
        public float HorizontalWidth { get { return 0; } }
        public float MinimumHeight { get { return 0; } }
        public float PaddingLeft { get { return 0; } }
        public float PaddingRight { get { return 0; } }
        public float PaddingTop { get { return 0; } }
        public float PaddingBottom { get { return 0; } }
    }
}
