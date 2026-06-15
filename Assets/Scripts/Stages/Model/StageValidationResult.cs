using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Stages.Model
{
    [Serializable]
    public sealed class StageValidationResult
    {
        public bool isValid = true;
        public List<string> messages = new List<string>();

        public void AddError(string message)
        {
            isValid = false;
            messages.Add(message);
        }
    }
}
