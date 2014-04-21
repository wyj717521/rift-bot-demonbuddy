using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.XmlEngine;
using Zeta.TreeSharp;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Bot.Profile;
using Trinity.DbProvider;
using Action = Zeta.TreeSharp.Action;

namespace RiftBot
{
    public partial class RiftBot : IPlugin
    {
        public Version Version { get { return new Version(0, 0, 7); } }
        public string Author { get { return "DyingHymn"; } }
        public string Description { get { return "Add support to rift objective detection"; } }
        public string Name { get { return "RiftBot"; } }
        public bool Equals(IPlugin other) { return (other.Name == Name) && (other.Version == Version); }
        private static string pluginPath = "";
        private static string sConfigFile = "";
        private static bool bSavingConfig = false;

        public void OnEnabled()
        {
            Logger.Log("RiftBot " + Version + " Enabled.");
        }

        public void OnDisabled()
        {
            Logger.Log("RiftBot " + Version + " Disabled.");
        }

        public void OnInitialize()
        {
            Logger.Log("RiftBot " + Version + " Initialized.");
        }

        public void OnPulse()
        {
        }

        public void OnShutdown()
        {

        }
        System.Windows.Window IPlugin.DisplayWindow
        {
            get
            {
                return null;
            }
        }

    }
	
    public static class Logger
    {
        private static readonly log4net.ILog Logging = Zeta.Common.Logger.GetLoggerInstanceForType();

        public static void Log(string message, params object[] args)
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            Logging.InfoFormat("[Rift Bot Plugin] " + string.Format(message, args), type.Name);
        }

        public static void Log(string message)
        {
            Log(message, string.Empty);
        }

    }

    [XmlElement("TrinityExploreRift")]
    public class TrinityExploreRift : Trinity.XmlTags.TrinityExploreDungeon
    {

        public TrinityExploreRift()
            : base()
        {
            if (PriorityScenes == null)
                PriorityScenes = new List<PrioritizeScene>();
			PriorityScenes.Add(new PrioritizeScene("Exit"));
			PriorityScenes.Add(new PrioritizeScene("Entrance"));
			PriorityScenes.Add(new PrioritizeScene("Portal"));
        }

        private bool _isDone = false;
        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(isRiftExploreCompleted(), base.CreateBehavior());
        }

        private Composite isRiftExploreCompleted()
        {
            return
            new PrioritySelector(
				new Decorator(ret => EndType == TrinityExploreEndType.RiftComplete && isRiftCompleted(),
					new Sequence(
						new Action(ret => Logger.Log("Rift is done. Tag Finished.")),
						new Action(ret => _isDone = true)
					)
				),
				new Decorator(ret => EndType == TrinityExploreEndType.RiftComplete && isHashVisible(),
					new Sequence(
						new Action(ret => Logger.Log("Rifting, exit found. Tag Finished.")),
						new Action(ret => _isDone = true)
					)
				)
			);
		}
		
		private bool isHashVisible()
        {	
			int __index = riftWorldIDs.IndexOf(ZetaDia.CurrentWorldId);
			if (__index < 0) {
				return false;
			}
            return ZetaDia.Minimap.Markers.CurrentWorldMarkers.Any(m => riftPortalHashes[__index] == m.NameHash && m.Position.Distance2D(Trinity.Trinity.Player.Position) <= MarkerDistance + 20f);
        }
		
        private DateTime __LastCheckRiftDone = DateTime.MinValue;
		
		private List<int> riftWorldIDs = new List<int>()
		{
			288454,
			288685,
			288687,
			288798,
			288800,
			288802,
			288804,
			288806,
		};
		
		private List<int> riftPortalHashes = new List<int>()
		{
			1938876094,
			1938876095,
			1938876096,
			1938876097,
			1938876098,
			1938876099,
			1938876100,
			1938876101,
			1938876102,
		};
		
        public bool isRiftCompleted()
        {
             if (DateTime.UtcNow.Subtract(__LastCheckRiftDone).TotalSeconds < 1)
                return false;

            __LastCheckRiftDone = DateTime.UtcNow;

            if (ZetaDia.Me.IsInBossEncounter)
            {
                return false;
            }
            
            if (ZetaDia.CurrentAct == Act.OpenWorld && riftWorldIDs.Contains(ZetaDia.CurrentWorldId))
            {			
                if (ZetaDia.ActInfo.AllQuests.Any(q => q.QuestSNO == 337492 && q.QuestStep != 10))
                    return false;

                if (ZetaDia.ActInfo.AllQuests.Any(q => q.QuestSNO == 337492 && q.QuestStep == 10))
                    return true;
            }
            return false;
        }

        public override bool IsDone
        {
            get { return _isDone || base.IsDone; }
        }

    }
	
    [XmlElement("RiftQuestAndStep")]
    public class RiftQuestAndStep : Trinity.XmlTags.BaseComplexNodeTag
    {
        protected override Composite CreateBehavior()
        {
            return
            new Decorator(ret => !IsDone,
                new PrioritySelector(
                    base.GetNodes().Select(b => b.Behavior).ToArray()
                )
            );
        }

        public override bool GetConditionExec()
        {
            return ZetaDia.ActInfo.AllQuests
                .Where(quest => quest.QuestSNO == QuestId && quest.State != QuestState.Completed && quest.QuestStep == StepId).FirstOrDefault() != null;
        }

        private bool CheckNotAlreadyDone(object obj)
        {
            return !IsDone;
        }
    }

} // namespace
