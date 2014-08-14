using System.Globalization;
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
        public HaloSplitSettings Settings { get; private set; }

        private TimerModel _timer;
        private LiveSplitState _state;
        private InfoTextComponent _deathCounter;
        private GameMemory _gameMemory;
        private DateTime? _splitTime;
        private int _deaths;

        public HaloSplitComponent(LiveSplitState state)
        {
            this.Settings = new HaloSplitSettings();
            this.ContextMenuControls = new Dictionary<String, Action>();
            _deathCounter = new InfoTextComponent("Death Count", "0");

            _state = state;
            _state.OnReset += state_OnReset;

            _timer = new TimerModel();
            _timer.CurrentState = state;

            _gameMemory = new GameMemory();
            // possible thread safety issues in all of these event handlers
            // invoking on main thread could lose a few frames of timer accuracy
            _gameMemory.OnMapChanged += gameMemory_OnMapChanged;
            _gameMemory.OnGainControl += gameMemory_OnGainControl;
            _gameMemory.OnLostControl += gameMemory_OnLostControl;
            _gameMemory.OnReset += gameMemory_OnReset;
            _gameMemory.OnPlayerDeath += gameMemory_OnPlayerDeath;
            _gameMemory.StartReading();
        }

        public void Dispose()
        {
            if (_gameMemory != null)
                _gameMemory.Stop();
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            if (!this.Settings.DeathCounter)
                return;

            string deaths = _deaths.ToString(CultureInfo.InvariantCulture);

            if (invalidator != null && _deathCounter.InformationValue != deaths)
            {
                _deathCounter.InformationValue = deaths;
                invalidator.Invalidate(0f, 0f, width, height);
            }
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region region)
        {
            this.PrepareDraw(state);
            _deathCounter.DrawVertical(g, state, width, region);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region region)
        {
            this.PrepareDraw(state);
            _deathCounter.DrawHorizontal(g, state, height, region);
        }

        void PrepareDraw(LiveSplitState state)
        {
            _deathCounter.NameLabel.ForeColor = state.LayoutSettings.TextColor;
            _deathCounter.ValueLabel.ForeColor = state.LayoutSettings.TextColor;
            _deathCounter.NameLabel.HasShadow = _deathCounter.ValueLabel.HasShadow = state.LayoutSettings.DropShadows;
        }

        void state_OnReset(object sender, TimerPhase t)
        {
            _deaths = 0;
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
                _deaths++;
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            return this.Settings.GetSettings(document);
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            return this.Settings;
        }

        public void SetSettings(XmlNode settings)
        {
            this.Settings.SetSettings(settings);
        }

        public void RenameComparison(string oldName, string newName) { }
        public float MinimumWidth    { get { return _deathCounter.MinimumWidth; } }
        public float MinimumHeight   { get { return _deathCounter.MinimumHeight; } }
        public float VerticalHeight  { get { return this.Settings.DeathCounter ? _deathCounter.VerticalHeight : 0; } }
        public float HorizontalWidth { get { return this.Settings.DeathCounter ? _deathCounter.HorizontalWidth : 0; } }
        public float PaddingLeft     { get { return this.Settings.DeathCounter ? _deathCounter.PaddingLeft : 0; } }
        public float PaddingRight    { get { return this.Settings.DeathCounter ? _deathCounter.PaddingRight : 0; } }
        public float PaddingTop      { get { return this.Settings.DeathCounter ? _deathCounter.PaddingTop : 0; } }
        public float PaddingBottom   { get { return this.Settings.DeathCounter ? _deathCounter.PaddingBottom : 0; } }
    }
}
