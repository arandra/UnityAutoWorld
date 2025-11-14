namespace Datas
{
    using AutoWorld.Core.Data;
    using UnityEngine;

    public class InitConsts : ScriptableObject
    {
        [SerializeField] private InitConst value = new InitConst();
        public InitConst Value => value;

        public void SetValue(InitConst newValue)
        {
            value = newValue;
        }
    }

}
