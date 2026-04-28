using System;
using System.Collections.Generic;
using UnityEngine;

namespace MobilOfl.Case
{
    [Serializable]
    public class SuspectData
    {
        [SerializeField] private string id = Guid.NewGuid().ToString();
        [SerializeField] private string displayName = "Supheli";
        [SerializeField] [TextArea] private string summary = "Supheli ozeti.";
        [SerializeField] private List<string> requiredEvidenceIds = new List<string>();

        public string Id => id;
        public string DisplayName => displayName;
        public string Summary => summary;
        public IReadOnlyList<string> RequiredEvidenceIds => requiredEvidenceIds;
    }
}
