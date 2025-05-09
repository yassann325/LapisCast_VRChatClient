
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;

namespace LapisCast{
    public class LapisCastBehaviour : UdonSharpBehaviour
    {
        private string _script_spacename = "default";
        private bool _subscribed = false;
        private LapisCastCore _lapisCastCore;

        // LapisCastBehaviourInit
        public void LapisCastBehaviourInit(){
            if(_subscribed){return;}
            _subscribed = true;
            _lapisCastCore = _serchLapisCastCore();
            if(_lapisCastCore){
                _lapisCastCore._subscribe_behaviour(this);
            }
        }
        public void LapisCastBehaviourInit(string spacename){
            SetSpaceName(spacename);
            if(_subscribed){return;}
            _subscribed = true;
            _lapisCastCore = _serchLapisCastCore();
            if(_lapisCastCore){
                _lapisCastCore._subscribe_behaviour(this);
            }
        }

        // SpaceName
        public void SetSpaceName(string spacename){
            if(spacename != null && spacename.Length != 0){
                _script_spacename = spacename;
            }
        }
        public string GetSpaceName(){
            return _script_spacename;
        }

        //LapisCastCore
        private LapisCastCore _serchLapisCastCore(){
            GameObject lcc_obj = GameObject.Find("LapisCast");
            if(!lcc_obj){
                Debug.LogError("[<color=#FF00FF>LapisCast</color>] Can't find LapisCast prefab");
                return null;
            }
            else{
                 return lcc_obj.GetComponent<LapisCastCore>();
            }
        }
        public LapisCastCore GetLapisCastCore(){
            return _lapisCastCore;
        }

        // SendMessage
        public void SendLapisCast(string keyname, DataToken value){
            _lapisCastCore.AddEvent(_script_spacename, keyname, value);
        }
        public void SendLapisCast(string spanename, string keyname, DataToken value){
            _lapisCastCore.AddEvent(spanename, keyname, value);
        }


        // Exec Event Self
        public void _triggerLapisEvent(string spanename, string keyname, DataToken value, bool sameinstance){
            OnLapisCastAllEvent(spanename, keyname, value, sameinstance);
            if(spanename == _script_spacename){
                OnLapisCastEvent(spanename, keyname, value, sameinstance);
            }      
        }

        public virtual void OnLapisCastEvent(string spanename, string keyname, DataToken value, bool sameinstance){

        }
        public virtual void OnLapisCastAllEvent(string spanename, string keyname, DataToken value, bool sameinstance){

        }
    }
}

