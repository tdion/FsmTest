using System;
using Stateless;
using Stateless.Graph;

namespace TelephoneCallExample
{
    public class PhoneCall
    {
        enum Trigger
        {
            CallDialed,
            CallConnected,
            LeftMessage,
            PlacedOnHold,
            TakenOffHold,
            PhoneHurledAgainstWall,
            MuteMicrophone,
            UnmuteMicrophone,
            SetVolume
        }

        enum State
        {
            OffHook,
            Ringing,
            Connected,
            OnHold,
            PhoneDestroyed
        }

        private State mState = State.OffHook;

        private readonly StateMachine<State, Trigger> mMachine;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<int> mSetVolumeTrigger;

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string> mSetCalleeTrigger;

        private readonly string mCaller;

        private string mCallee;

        public PhoneCall(string caller)
        {
            mCaller = caller;
            mMachine = new StateMachine<State, Trigger>(() => mState, s => mState = s);

            mSetVolumeTrigger = mMachine.SetTriggerParameters<int>(Trigger.SetVolume);
            mSetCalleeTrigger = mMachine.SetTriggerParameters<string>(Trigger.CallDialed);

            mMachine.Configure(State.OffHook)
	            .Permit(Trigger.CallDialed, State.Ringing);

            mMachine.Configure(State.Ringing)
                .OnEntryFrom(mSetCalleeTrigger, callee => OnDialed(callee), "Caller number to call")
	            .Permit(Trigger.CallConnected, State.Connected);

            mMachine.Configure(State.Connected)
                .OnEntry(t => StartCallTimer())
                .OnExit(t => StopCallTimer())
                .InternalTransition(Trigger.MuteMicrophone, t => OnMute())
                .InternalTransition(Trigger.UnmuteMicrophone, t => OnUnmute())
                .InternalTransition<int>(mSetVolumeTrigger, (volume, t) => OnSetVolume(volume))
                .Permit(Trigger.LeftMessage, State.OffHook)
	            .Permit(Trigger.PlacedOnHold, State.OnHold);

            mMachine.Configure(State.OnHold)
                .SubstateOf(State.Connected)
                .Permit(Trigger.TakenOffHold, State.Connected)
                .Permit(Trigger.PhoneHurledAgainstWall, State.PhoneDestroyed);

            mMachine.OnTransitioned(t => Console.WriteLine($"OnTransitioned: {t.Source} -> {t.Destination} via {t.Trigger}({string.Join(", ",  t.Parameters)})"));
        }

        private void OnSetVolume(int volume)
        {
            Console.WriteLine("Volume set to " + volume + "!");
        }

        private void OnUnmute()
        {
            Console.WriteLine("Microphone unmuted!");
        }

        private void OnMute()
        {
            Console.WriteLine("Microphone muted!");
        }

        private void OnDialed(string callee)
        {
            mCallee = callee;
            Console.WriteLine("[Phone Call] placed for : [{0}]", mCallee);
        }

        private void StartCallTimer()
        {
            Console.WriteLine("[Timer:] Call started at {0}", DateTime.Now);
        }

        private void StopCallTimer()
        {
            Console.WriteLine("[Timer:] Call ended at {0}", DateTime.Now);
        }

        public void Mute()
        {
            mMachine.Fire(Trigger.MuteMicrophone);
        }

        public void UnMute()
        {
            mMachine.Fire(Trigger.UnmuteMicrophone);
        }

        public void SetVolume(int volume)
        {
            mMachine.Fire(mSetVolumeTrigger, volume);
        }

        public void Print()
        {
            Console.WriteLine("[{1}] placed call and [Status:] {0}", mMachine.State, mCaller);
        }

        public void Dialed(string callee)
        {           
            mMachine.Fire(mSetCalleeTrigger, callee);
        }

        public void Connected()
        {
            mMachine.Fire(Trigger.CallConnected);
        }

        public void Hold()
        {
            mMachine.Fire(Trigger.PlacedOnHold);
        }

        public void Resume()
        {
            mMachine.Fire(Trigger.TakenOffHold);
        }

        public string ToDotGraph()
        {
            return UmlDotGraph.Format(mMachine.GetInfo());
        }
    }
}