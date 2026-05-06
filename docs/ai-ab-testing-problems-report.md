# AI A/B Testing Problems Report

Date: 2026-05-06

Scope: InterviewService gRPC end-to-end AI tests using `InterviewGrpcClientTester`, with Grok 4.20 as the strongest current interviewer candidate and GPT-OSS 120B as the default simulated user/tester.

## Executive Summary

The main problem found during A/B testing is not that Grok asks too few questions. Grok usually asks enough follow-up questions for a usable onboarding interview. The bigger issue is that the final `UserProfile` sometimes compresses away explicit evidence from the transcript, especially selected tools, domains, secondary skills, and auxiliary responsibilities.

Prompt-only changes did not reliably fix this. Some prompt variants improved one or two scenarios, but they also caused regressions elsewhere. The most promising next step is a structural validation/repair pass after profile generation, comparing the final profile against transcript facts before accepting it.

## Test Runs Referenced

Reports are stored under:

`C:\Users\meeuc\OneDrive\Desktop\FitFlow\AITesting\InterviewGrpcClientTester`

Key folders:

- `PromptAB_Grok420_BaselineClean_2026-05-05_6Reports`
- `PromptAB_Grok420_CoverageChecklist_2026-05-06_6Reports`
- `PromptAB_Grok420_EvidenceRetention_Retry_2026-05-06_6Reports`
- `PromptAB_Grok420_SelectedOptionExtraction_2026-05-06_6Reports`
- `ReviewerBias_GrokInterviewer_5Scenarios_2026-05-06`
- earlier model/timing samples for Gemma, Grok, GLM, Gemini, and Mistral

## Problems Found

### 1. Final profile drops transcript evidence

Observed repeatedly across design and IT scenarios.

Examples:

- Security profile omitted many explicitly selected tools such as Burp Suite, SonarQube, Semgrep, Snyk, Checkmarx, Veracode, GitHub Advanced Security, AWS Security Hub/GuardDuty, Terraform, Splunk/ELK, and MITRE ATT&CK-related tools.
- Data engineering profile omitted cloud platforms and storage systems in some runs, such as AWS, GCP, Azure, Snowflake, Redshift, BigQuery, Flink, DataDog, Prometheus, and Great Expectations.
- Design profiles often missed secondary-but-real skills such as accessibility, information architecture, motion/micro-interactions, mentoring, analytics, or additional tools.

Impact:

- The interview transcript often contains enough information, but the final model output loses it.
- This weakens matching quality even when the interview conversation itself is good.

Likely root cause:

- The model is compressing the final `UserProfile` too aggressively.
- Prompt instructions alone are not deterministic enough to preserve all selected facts.

Recommended fix:

- Add a post-generation validation/repair step:
  - Extract explicit facts from transcript.
  - Compare them against `UserProfile`.
  - Ask AI to repair missing but supported facts, or implement deterministic extraction for selected option groups.

### 2. Prompt-only fixes were unstable

Prompt variants tested:

| Variant | Reports | Failures | Avg score | Avg AI step time | Result |
| --- | ---: | ---: | ---: | ---: | --- |
| Baseline | 6 | 0 | 8.00 | 5.97s | Best stable production choice |
| Coverage checklist | 6 | 0 | 7.83 | 7.03s | Worse and slower |
| Evidence retention | 6 | 0 | 7.83 | 7.08s | Worse and slower |
| Selected option extraction | 6 | 0 | 8.00 | 5.83s | Mixed: two 9/10 wins, one 6/10 regression |

The selected-option extraction prompt looked promising in backend/data-engineering cases, but regressed badly on security engineering by dropping most tools. Because of that variance, it was not kept.

Impact:

- Prompt-only optimization is noisy and risky.
- Averages can hide severe per-domain regressions.

Recommended fix:

- Keep the baseline prompt for now.
- Move reliability-critical evidence preservation out of prompt wording and into validation/repair logic.

### 3. Reviewer scores are model-biased

A reviewer-bias test ran the same 5 final profiles through multiple reviewer models.

| Reviewer | Avg score | Pattern |
| --- | ---: | --- |
| Grok 4.20 | 7.2 | Harshest |
| GPT-OSS 120B | 8.0 | Middle, usable |
| GLM 4.5 Air | about 8.2 | Slightly generous |
| Gemma 4 31B | 9.0 | Too forgiving |

Impact:

- Review score is not an objective metric.
- GPT-OSS does not appear unusually harsh.
- Gemma appears too forgiving for quality gating.

Recommended fix:

- Keep GPT-OSS as the main reviewer for now.
- For stronger validation, use GPT-OSS + Grok and inspect cases where they disagree strongly.
- Avoid using Gemma alone as a reviewer.

### 4. Some models conclude too early

This was more visible with weaker/faster interviewer candidates, especially Gemini Flash Lite.

Observed pattern:

- Gemini Flash Lite sometimes produced very short interviews and shallow profiles.
- A guard/lean prompt improved depth somewhat but introduced noise/failures.

Grok-specific finding:

- Grok did not show the same severe early-conclusion behavior.
- Timed Grok runs usually had 4-5 AI-generated interviewer steps after the required first question.
- Older Grok reports had 5-7 total transcript questions even when timing metadata was missing.

Impact:

- Model choice matters more than small prompt adjustments for interview depth.

Recommended fix:

- Use Grok as the default interviewer for now.
- Keep minimum-depth guard as a possible application-level fallback, but do not force it through prompt variants unless there is repeated early-conclusion evidence.

### 5. Tester-side answer generation sometimes returns invalid JSON

Warnings appeared during several runs:

- Empty response where JSON was expected.
- `textAnswer` returned as a non-string.
- `selectedOptions` containing invalid `OptionAnswer` shapes.

The retry/validation logic usually recovered, but it increased latency and sometimes made reports noisy.

Impact:

- Test failures or delays can be caused by tester-side AI, not InterviewService.
- This can distort model/prompt evaluation.

Recommended fix:

- Keep AI validators enabled.
- Improve tester prompt/schema pressure around `Answer`.
- Consider a deterministic fallback answer generator for simple option-only questions during tests.

### 6. Provider budget/rate limits can invalidate batches

Polza returned:

`HTTP 402 (: INSUFFICIENT_BALANCE)` / `Достигнут лимит по сумме`

This caused server-side `Answer` failures in one attempted evidence-retention run.

Impact:

- Failed A/B batches can look like prompt/model failures when they are actually provider/account failures.

Recommended fix:

- Detect provider payment/rate-limit errors explicitly.
- Surface clearer gRPC errors instead of generic `Unknown`.
- In test reports, classify provider-budget failures separately from model-quality failures.

### 7. gRPC/API error reporting is too generic for AI failures

When the AI returned no valid `FormElement`, the gRPC client saw:

`StatusCode="Unknown", Detail="Exception was thrown by handler."`

Impact:

- Hard to distinguish bad AI output, provider budget failure, validation failure, and real service bugs from the client side.

Recommended fix:

- Map expected AI/provider failures to clearer gRPC statuses:
  - provider unavailable/budget/rate limit -> `Unavailable` or `ResourceExhausted`
  - invalid AI response after retries -> `Internal` with sanitized details
  - invalid user answer -> `InvalidArgument`

### 8. Test infrastructure can distort runtime conclusions

Observed during the longer experimentation:

- Docker Desktop/WSL temporarily wedged and blocked compose commands.
- Parallel runs worked well when Docker was healthy.
- Multi-reviewer reports take longer because each completed interview performs several final review calls.

Impact:

- Wall-clock batch time is not always equal to interviewer model latency.
- Timing should be based on recorded `AI interviewer step timings ms`, not whole container runtime.

Recommended fix:

- Keep per-step timing in reports.
- Add reviewer timing separately if reviewer model performance becomes important.

## Current Recommendation

Use this production/testing stance for now:

1. Grok 4.20 as interviewer.
2. Baseline prompt, no prompt variant logic in production.
3. GPT-OSS 120B as primary reviewer.
4. Add transcript-vs-profile validation/repair before investing much more in prompt tuning.
5. Treat selected option preservation as an application correctness concern, not just a prompt preference.

## Suggested Next Engineering Tasks

1. Add a `UserProfile` validation/repair use case in Application.
2. Add structured extraction of selected option facts from transcript.
3. Add clearer gRPC error mapping for AI/provider failures.
4. Make tester reports classify failures by category: provider, tester-answer, interviewer-output, gRPC/service.
5. Add reviewer disagreement reporting when multiple reviewers are configured.
