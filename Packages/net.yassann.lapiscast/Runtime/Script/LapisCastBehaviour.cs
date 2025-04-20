
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;

namespace LapisCast{
    public class LapisCastBehaviour : UdonSharpBehaviour
    {
        private string script_spacename = "every";
        private bool _subscribed = false;
        private LapisCastCore lapisCastCore;

        public void LapisCastBehaviourInit(){
            if(_subscribed){return;}
            _subscribed = true;
            lapisCastCore = GetLapisCastCore();
            if(lapisCastCore){
                lapisCastCore._subscribe_behaviour(this);
            }
        }
        public void LapisCastBehaviourInit(string spacename){
            SetSpaceName(spacename);
            if(_subscribed){return;}
            _subscribed = true;
            lapisCastCore = GetLapisCastCore();
            if(lapisCastCore){
                lapisCastCore._subscribe_behaviour(this);
            }
        }
        public void SetSpaceName(string spacename){
            if(spacename != null){
                script_spacename = spacename;
            }
        }
        private LapisCastCore GetLapisCastCore(){
            GameObject lcc_obj = GameObject.Find("LapisCast");
            if(!lcc_obj){
                Debug.LogError("[<color=#FF00FF>LapisCast</color>] Can't find LapisCast prefab");
                return null;
            }
            else{
                 return lcc_obj.GetComponent<LapisCastCore>();
            }
        }


        public void SendLapisCast(string keyname, DataToken value){
            lapisCastCore.AddEvent(script_spacename, keyname, value);
        }
        public void SendLapisCast(string spanename, string keyname, DataToken value){
            lapisCastCore.AddEvent(spanename, keyname, value);
        }


        public void _triggerLapisEvent(string spanename, string keyname, DataToken value, bool sameinstance){
            OnLapisCastAllEvent(spanename, keyname, value, sameinstance);
            if(spanename == script_spacename){
                OnLapisCastEvent(spanename, keyname, value, sameinstance);
            }      
        }

        public virtual void OnLapisCastEvent(string spanename, string keyname, DataToken value, bool sameinstance){

        }
        public virtual void OnLapisCastAllEvent(string spanename, string keyname, DataToken value, bool sameinstance){

        }
    }
}

