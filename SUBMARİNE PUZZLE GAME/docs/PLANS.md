\# Refactoring Plans



Significant architectural changes must be planned before implementation.



A significant refactor includes changes that:



\- affect multiple systems

\- change module boundaries

\- alter networking architecture

\- modify important player systems

\- affect many files

\- change public APIs

\- create new abstractions

\- introduce serialization risk



\---



\# Required Plan Format



Each plan should include:



\## Problem



Describe the current architectural or code-quality problem.



\## Evidence



List the relevant:



\- files

\- classes

\- methods

\- dependencies



\## Proposed Change



Explain the intended architecture.



\## Files Affected



List the expected files.



\## Networking Impact



Explicitly state whether the change affects:



\- PurrNet RPCs

\- ownership

\- NetworkIdentity

\- synchronization

\- authority

\- Network Rules

\- prediction



If none are affected, state that clearly.



\## Unity Serialization Risk



Check:



\- serialized fields

\- Prefabs

\- Scenes

\- ScriptableObjects

\- UnityEvents



\## Behaviour Risk



Explain any possible gameplay regression.



\## Implementation Steps



Break the change into small independently understandable steps.



\## Verification



Describe how the result should be checked.



\---



\# Refactoring Order



Prefer:



1\. characterization / understanding

2\. small safe cleanup

3\. dependency separation

4\. architecture improvement

5\. final verification



Do not combine unrelated refactors.



\---



\# Networking Refactors



Networking changes must be treated as higher risk.



Before changing network code:



1\. identify caller

2\. identify execution side

3\. identify owner

4\. identify authority requirements

5\. identify synchronization requirements

6\. identify observer behavior

7\. identify host-specific behavior



Do not change networking behavior implicitly.



\---



\# Approval Boundary



General Clean Code refactors may proceed when behaviour is preserved.



Changes involving:



\- authority model

\- ownership model

\- network protocol

\- RPC direction

\- gameplay rules

\- prediction model



must be presented explicitly as architectural changes rather than hidden inside cleanup work.

