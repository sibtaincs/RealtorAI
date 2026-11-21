---
description: Run the full local verification loop
---

Follow AGENTS.md. Verify in this order and report failures with the exact command that failed; do not change files yet:

!`dotnet build backend/RealEstateAI.Api.csproj`
!`dotnet test tests/RealEstateAI.Api.Tests/RealEstateAI.Api.Tests.csproj`
!`cd frontend && npm run build`
