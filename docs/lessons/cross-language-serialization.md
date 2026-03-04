---
topic: Cross-language serialization
tags: [json, serialization, csharp, javascript, cloud-code, jsonutility]
---

# Cross-Language Serialization

## C# struct fields must match JSON key casing exactly with JsonUtility
<!-- issue: #20 | pr: #21 -->
- Unity's `JsonUtility` (used by Cloud Code SDK's `CallEndpointAsync<T>`) is case-sensitive
- C# convention (PascalCase) does not match JS convention (camelCase/lowercase) -- fields will silently bind to default values
- When a C# DTO deserializes JSON from a JS service, use lowercase public fields matching the JS output
- Document the deviation from coding conventions (e.g., "no public fields") directly on the struct with a comment explaining why
- Plan-reviewer caught this before implementation; without the review gate it would have been a silent runtime bug
