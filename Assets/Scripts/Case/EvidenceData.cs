using System;
using UnityEngine;

namespace MobilOfl.Case
{
    public enum EvidenceCategory
    {
        Document,
        Digital,
        Physical,
        Witness
    }

    [Serializable]
    public class EvidenceData
    {
        [SerializeField] private string id = Guid.NewGuid().ToString();
        [SerializeField] private string title = "Yeni Delil";
        [SerializeField] [TextArea] private string description = "Delil aciklamasi.";
        [SerializeField] private EvidenceCategory category = EvidenceCategory.Document;
        [SerializeField] private Sprite icon;
        [SerializeField] private bool isCritical;

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public EvidenceCategory Category => category;
        public Sprite Icon => icon;
        public bool IsCritical => isCritical;
    }
}
