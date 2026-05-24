using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.AI.Assistant.Editor;
using Unity.AI.Assistant.Editor.Analytics;
using Unity.AI.Assistant.Editor.Utils.Event;
using Unity.AI.Assistant.FunctionCalling;
using Unity.AI.Assistant.UI.Editor.Scripts.Components.UserInteraction;
using Unity.AI.Assistant.UI.Editor.Scripts.Events;
using Unity.AI.Assistant.UI.Editor.Scripts.Markup;
using Unity.AI.Assistant.UI.Editor.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components.ChatElements
{
    class ExitPlanModeInteractionElement : InteractionContentView, IInteractionSource<string>
    {
        internal static event Action PlanApproved;

        public string Title => "Plan ready to execute";
        public event Action<string> OnCompleted;
        public event Action<bool> ExpandedStateChanged;
        public TaskCompletionSource<string> TaskCompletionSource { get; } = new();

        readonly string m_PlanPath;
        readonly string m_PlanContent;
        readonly string m_PlanTitle;

        public string PlanPath => m_PlanPath;
        public string PlanContent => m_PlanContent;
        public string PlanTitle => m_PlanTitle;

        const string k_CopyIconClass = "mui-icon-copy";
        const string k_CheckmarkIconClass = "mui-icon-checkmark";

        bool m_Completed;
        bool m_ExpandedPanelOpen;
        bool m_RestoreExpandedRequested;
        Button m_CopyIconButton;
        Image m_CopyIconImage;
        CancellationTokenSource m_CopyActiveTokenSource;
        Label m_StatusLabel;
        PlanReviewHeaderActions m_HeaderActions;
        BaseEventSubscriptionTicket m_ExpandedPanelOpenedSubscription;
        BaseEventSubscriptionTicket m_ExpandedPanelClosedSubscription;

        public ExitPlanModeInteractionElement(string planPath, string planContent, string title = null)
        {
            m_PlanPath = planPath ?? string.Empty;
            m_PlanTitle = string.IsNullOrWhiteSpace(title) ? "Implementation Plan" : title;
            m_PlanContent = planContent ?? string.Empty;
        }

        protected override void InitializeView(TemplateContainer view)
        {
            view.Q<Label>("planTitleLabel").text = m_PlanTitle;

            var pathLabel = view.Q<Label>("pathLabel");
            pathLabel.text = m_PlanPath;

            var scrollView = view.Q<ScrollView>("planContentScroll");
            var markdownElements = new List<VisualElement>();
            MarkdownAPI.MarkupText(Context, m_PlanContent, null, markdownElements, null);
            foreach (var el in markdownElements)
                scrollView.Add(el);

            m_CopyIconButton = view.SetupButton("planCopyButton", _ => OnCopyClicked());
            m_CopyIconImage = view.Q<Image>("planCopyIcon");
            view.SetupButton("expandButton", _ => OnExpandClicked());

            m_StatusLabel = view.Q<Label>("statusLabel");
            m_StatusLabel.SetDisplay(false);

            RegisterAttachEvents(OnAttach, OnDetach);
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            m_ExpandedPanelOpenedSubscription = AssistantEvents.Subscribe<EventExpandedPanelOpened>(OnExpandedPanelOpened);
            m_ExpandedPanelClosedSubscription = AssistantEvents.Subscribe<EventExpandedPanelClosed>(OnExpandedPanelClosed);

            if (m_RestoreExpandedRequested)
            {
                m_RestoreExpandedRequested = false;
                if (!m_ExpandedPanelOpen && !m_Completed)
                    RequestExpand();
            }
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            AssistantEvents.Unsubscribe(ref m_ExpandedPanelOpenedSubscription);
            AssistantEvents.Unsubscribe(ref m_ExpandedPanelClosedSubscription);

            m_CopyActiveTokenSource?.Cancel();
            m_CopyActiveTokenSource?.Dispose();
            m_CopyActiveTokenSource = null;
        }

        void OnExpandedPanelOpened(EventExpandedPanelOpened evt)
        {
            if (m_HeaderActions != null)
            {
                m_ExpandedPanelOpen = true;
                ExpandedStateChanged?.Invoke(true);
            }
        }

        void OnExpandedPanelClosed(EventExpandedPanelClosed evt)
        {
            // Gate on m_HeaderActions so a different element's expanded panel closing does not
            // overwrite our persisted expanded=true with false.
            if (m_HeaderActions == null) return;

            m_ExpandedPanelOpen = false;
            ExpandedStateChanged?.Invoke(false);
            m_HeaderActions.DenyClicked -= OnDeny;
            m_HeaderActions.ApproveClicked -= OnApprove;
            m_HeaderActions = null;
        }

        void OnCopyClicked()
        {
            GUIUtility.systemCopyBuffer = m_PlanContent;
            m_CopyIconImage.RemoveFromClassList(k_CopyIconClass);
            m_CopyIconImage.AddToClassList(k_CheckmarkIconClass);
            m_CopyIconButton.EnableInClassList(AssistantUIConstants.ActiveActionButtonClass, true);
            TimerUtils.DelayedAction(ref m_CopyActiveTokenSource, () =>
            {
                m_CopyIconButton.EnableInClassList(AssistantUIConstants.ActiveActionButtonClass, false);
                m_CopyIconImage.RemoveFromClassList(k_CheckmarkIconClass);
                m_CopyIconImage.AddToClassList(k_CopyIconClass);
            });
        }

        void OnExpandClicked() => RequestExpand();

        void RequestExpand()
        {
            if (m_Completed || m_ExpandedPanelOpen)
                return;

            var expandedContent = new PlanReviewExpandedContent(m_PlanContent);
            expandedContent.Initialize(Context);

            m_HeaderActions = new PlanReviewHeaderActions(m_PlanContent);
            m_HeaderActions.Initialize(Context);
            m_HeaderActions.DenyClicked += OnDeny;
            m_HeaderActions.ApproveClicked += OnApprove;

            AssistantEvents.Send(new EventExpandedViewRequested(
                m_PlanTitle,
                expandedContent,
                m_HeaderActions,
                ScrollViewMode.Vertical));
        }

        internal void MarkRestoreExpanded() => m_RestoreExpandedRequested = true;

        internal void OnApprove()
        {
            if (m_Completed)
                return;
            m_Completed = true;

            ShowCompletionState("✓ Plan approved");

            var approvalMode = "agent";
            var modeDescription = "Agent mode";

            var result = JsonConvert.SerializeObject(new
            {
                approved = true,
                approvalMode,
                message = $"Plan approved. Switching to {modeDescription}.\n\n" +
                          $"The approved implementation plan is stored at: {m_PlanPath}\n" +
                          "Stop here — do not call any more tools or attempt implementation.\n" +
                          "Ask the user if they want to execute the entire plan. If yes, " +
                          "read and follow the plan strictly during implementation."
            });

            AIAssistantAnalytics.ReportUITriggerLocalPlanReviewApprovedEvent(Context.Blackboard.ActiveConversationId, m_PlanPath);

            PlanApproved?.Invoke();
            SetResult(result);
            InvokeCompleted();
            AssistantEvents.Send(new EventExpandedPanelCloseRequested());
        }

        internal void OnDeny()
        {
            if (m_Completed)
                return;
            m_Completed = true;

            ShowCompletionState("Plan denied");

            AIAssistantAnalytics.ReportUITriggerLocalPlanReviewDeniedEvent(Context.Blackboard.ActiveConversationId, m_PlanPath);

            var result = JsonConvert.SerializeObject(new
            {
                approved = false,
                message = "The user chose not to proceed with this plan. Treat this as a hard stop — do not revise, re-present, or re-attempt planning unless the user explicitly asks. Wait for the user to provide new direction."
            });
            SetResult(result);
            InvokeCompleted();
            AssistantEvents.Send(new EventExpandedPanelCloseRequested());
        }

        internal void OnRevise(string feedback)
        {
            if (m_Completed) return;
            m_Completed = true;

            ShowCompletionState("Plan revision requested");

            var result = JsonConvert.SerializeObject(new
            {
                approved = false,
                revise = true,
                feedback,
                message = $"The user wants to revise the plan with the feedback above. " +
                          $"Update the plan file at {m_PlanPath} accordingly, then call Unity.ExitPlanMode again with the revised plan."
            });
            SetResult(result);
            InvokeCompleted();
            AssistantEvents.Send(new EventExpandedPanelCloseRequested());
        }

        void ShowCompletionState(string statusText)
        {
            m_StatusLabel.text = statusText;
            m_StatusLabel.SetDisplay(true);
        }

        void SetResult(string result)
        {
            TaskCompletionSource.TrySetResult(result);
            OnCompleted?.Invoke(result);
        }

        public void CancelInteraction()
        {
            if (m_Completed) return;
            m_Completed = true;

            TaskCompletionSource.TrySetCanceled();
            InvokeCompleted();
            AssistantEvents.Send(new EventExpandedPanelCloseRequested());
        }

        /// <summary>
        /// Reads a plan file from disk, trying both the raw path and a project-relative resolution.
        /// </summary>
        [ToolPermissionIgnore]
        internal static string ReadPlanFile(string planPath)
        {
            if (string.IsNullOrEmpty(planPath))
                return "(No plan path provided)";

            try
            {
                var fullPath = Path.GetFullPath(planPath);
                var fullDataPath = Path.GetFullPath(Application.dataPath);

                if (!fullPath.StartsWith(fullDataPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    && !fullPath.Equals(fullDataPath, StringComparison.OrdinalIgnoreCase))
                    return "(Plan file outside project's Assets directory)";

                if (File.Exists(fullPath))
                    return File.ReadAllText(fullPath);

                return $"(Plan file not found: {planPath})";
            }
            catch (Exception e)
            {
                return $"(Error reading plan file: {e.Message})";
            }
        }
    }
}
