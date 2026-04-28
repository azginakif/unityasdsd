using System;
using System.Collections.Generic;
using MobilOfl.Case;
using UnityEngine;

namespace MobilOfl.Gameplay
{
    public class CaseProgressTracker : MonoBehaviour
    {
        [Serializable]
        public class ProgressStep
        {
            public string Id;
            public string Label;
            public bool IsCompleted;
        }

        [SerializeField] private int minimumNpcInterviews = 2;

        private readonly List<ProgressStep> _steps = new List<ProgressStep>();
        private CaseSessionManager _session;

        public IReadOnlyList<ProgressStep> Steps => _steps;

        private void Awake()
        {
            ResetSteps();
        }

        private void OnEnable()
        {
            TrySubscribe();
            RefreshProgress();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (_session == null)
            {
                TrySubscribe();
            }
        }

        public float GetCompletionRatio()
        {
            if (_steps.Count == 0)
            {
                return 0f;
            }

            var completedCount = 0;
            for (var i = 0; i < _steps.Count; i++)
            {
                if (_steps[i].IsCompleted)
                {
                    completedCount++;
                }
            }

            return (float)completedCount / _steps.Count;
        }

        private void TrySubscribe()
        {
            if (CaseSessionManager.Instance == null || _session == CaseSessionManager.Instance)
            {
                return;
            }

            Unsubscribe();
            _session = CaseSessionManager.Instance;
            _session.CaseStarted += HandleCaseStarted;
            _session.EvidenceCollected += HandleEvidenceCollected;
            _session.CaseResolved += HandleCaseResolved;
            RefreshProgress();
        }

        private void Unsubscribe()
        {
            if (_session == null)
            {
                return;
            }

            _session.CaseStarted -= HandleCaseStarted;
            _session.EvidenceCollected -= HandleEvidenceCollected;
            _session.CaseResolved -= HandleCaseResolved;
            _session = null;
        }

        private void HandleCaseStarted(CaseDefinition _)
        {
            ResetSteps();
            RefreshProgress();
        }

        private void HandleEvidenceCollected(EvidenceData _)
        {
            RefreshProgress();
        }

        private void HandleCaseResolved(bool _, string __)
        {
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            if (_session == null || _session.ActiveCase == null)
            {
                return;
            }

            SetStepState("find_first_evidence", _session.CollectedEvidenceIds.Count > 0);
            SetStepState("critical_evidence", _session.CollectedCriticalEvidenceCount >= _session.TotalCriticalEvidenceCount && _session.TotalCriticalEvidenceCount > 0);
            SetStepState("interview_npcs", _session.InterviewedNpcCount >= minimumNpcInterviews);
            SetStepState("accuse_suspect", _session.IsCaseResolved);
        }

        private void ResetSteps()
        {
            _steps.Clear();
            _steps.Add(new ProgressStep { Id = "find_first_evidence", Label = "Ilk delili bul" });
            _steps.Add(new ProgressStep { Id = "critical_evidence", Label = "Tum kritik delilleri topla" });
            _steps.Add(new ProgressStep { Id = "interview_npcs", Label = "En az 2 NPC ile konus" });
            _steps.Add(new ProgressStep { Id = "accuse_suspect", Label = "Dogru supheliyi sucla" });
        }

        private void SetStepState(string stepId, bool isCompleted)
        {
            for (var i = 0; i < _steps.Count; i++)
            {
                if (_steps[i].Id == stepId)
                {
                    _steps[i].IsCompleted = isCompleted;
                    return;
                }
            }
        }
    }
}
