## 🏗️ Implementation Plan

> **Agent:** `software-architect`

**Approach**: Add a `build` stage to `.gitlab-ci.yml` between `test` and `deploy` that runs the existing `WebGLBuildScript.Build()` inside a GameCI Docker image targeting WebGL. The deploy job then consumes the build artifacts. Update `ci-cd-gotchas.md` to use GitLab CI variable syntax instead of the current GitHub Actions `${{ secrets.* }}` syntax.

**Key decisions**:
- Use `unityci/editor:6000.3.10f1-webgl-3` Docker image directly (GameCI's GitHub Action wrapper doesn't exist for GitLab — use the raw image with `unity-editor` CLI in batchmode)
- Invoke `WebGLBuildScript.Build` via `-executeMethod` — the script already exists at `Assets/Editor/WebGLBuildScript.cs` and reads `BUILD_PATH` env var, defaulting to `build/WebGL/golf-game`
- Use GitLab CI `artifacts:` to pass `build/WebGL/golf-game/` from build → deploy
- Disk cleanup via `rm -rf` of known large directories instead of a GitHub Action (no equivalent for GitLab)

### Files to modify
- `.gitlab-ci.yml` — add `build` stage, add `webgl-build` job, update `deploy` job path and dependency
- `docs/ci-cd-gotchas.md` — replace GitHub Actions `${{ secrets.* }}` syntax with GitLab CI `$VARIABLE` syntax; replace GH Actions disk cleanup step with shell commands

### Steps

**1. Add `build` stage to `.gitlab-ci.yml`**
- File: `.gitlab-ci.yml`
- Insert `build` between `test` and `deploy` in the `stages:` list:
  ```yaml
  stages:
    - test
    - build
    - deploy
  ```

**2. Add `webgl-build` job**
- File: `.gitlab-ci.yml`
- Add job after `cloud-code-test`, before `deploy`:
  ```yaml
  webgl-build:
    stage: build
    image: unityci/editor:6000.3.10f1-webgl-3
    variables:
      BUILD_PATH: build/WebGL/golf-game
    before_script:
      # Free disk space — Unity 6 images are ~15GB
      - rm -rf /usr/share/dotnet /usr/local/lib/android /opt/ghc || true
      # Activate Unity license
      - unity-editor -batchmode -nographics -quit
          -manualLicenseFile <(echo "$UNITY_LICENSE")
          -logFile /dev/stdout || true
    script:
      - git lfs install && git lfs pull
      - rm -f .lfs-assets-id
      - unity-editor -batchmode -nographics -quit
          -projectPath .
          -executeMethod WebGLBuildScript.Build
          -buildTarget WebGL
          -logFile /dev/stdout
    artifacts:
      paths:
        - build/WebGL/golf-game/
      expire_in: 1 day
    only:
      - main
      - merge_requests
  ```
- Key details:
  - `unity-editor` is the binary name inside GameCI Docker images (not `/Applications/Unity/...`)
  - License activation uses `$UNITY_LICENSE` CI variable (base64 `.ulf` content) — the `before_script` activates it before the build
  - `git lfs pull` + `rm -f .lfs-assets-id` handles the dirty build error per `ci-cd-gotchas.md`
  - Artifacts scoped to `build/WebGL/golf-game/` with 1-day expiry
  - Runs on `main` and MRs so build breakage is caught pre-merge

**3. Update `deploy` job to consume build artifacts**
- File: `.gitlab-ci.yml`
- Add `needs: [webgl-build]` to the deploy job so it depends on the build stage
- Update the deploy path from `build/` to `build/WebGL/golf-game/`:
  ```yaml
  deploy:
    stage: deploy
    image: node:20
    needs: [webgl-build]
    script:
      - npm install -g wrangler
      - wrangler pages deploy build/WebGL/golf-game/ --project-name=golf-game
    environment:
      name: production
    only:
      - main
    when: manual
  ```

**4. Update `ci-cd-gotchas.md` — GitLab CI variable syntax**
- File: `docs/ci-cd-gotchas.md`
- Replace the "GameCI Requires All Three Secrets" section:
  - Change `${{ secrets.UNITY_LICENSE }}` → `$UNITY_LICENSE` (and same for EMAIL, PASSWORD)
  - Change from `env:` block (GH Actions) to `variables:` block (GitLab CI)
- Replace the "Disk Space" section:
  - Remove the GitHub Actions `uses: jlumbroso/free-disk-space@v1.3.1` block
  - Replace with shell commands: `rm -rf /usr/share/dotnet /usr/local/lib/android /opt/ghc || true`
- Replace the "LFS Dirty Build Error" section:
  - Change from `- run: |` (GH Actions) to plain shell script syntax

### Testing

**Test strategy**: CI pipeline self-validation — the MR pipeline will exercise the new build job.

**Test cases** (manual verification — no unit tests for CI config):
- `build job triggers on MR`: Push branch, verify `webgl-build` job appears in pipeline
- `build job fails without license`: If `UNITY_LICENSE` CI variable is not set, job should fail with clear license error (not a cryptic crash)
- `build job succeeds with license`: Once CI variables are configured, build completes and produces `build/WebGL/golf-game/index.html`
- `deploy job receives artifacts`: Deploy job (manual trigger on main) can access `build/WebGL/golf-game/` from the build stage
- `deploy path correct`: Wrangler deploys from `build/WebGL/golf-game/` not `build/`
- `existing test jobs unaffected`: `lint` and `cloud-code-test` jobs still run and pass

**Manual verification**:
- Confirm `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` are set as CI/CD variables in GitLab project settings (Settings > CI/CD > Variables). **If not yet configured, flag in MR description.**
- After first successful build, verify the artifacts contain `index.html`, `Build/` directory, and `TemplateData/` (standard Unity WebGL output structure)

### Risks
- **GameCI Docker image tag may not exist**: `unityci/editor:6000.3.10f1-webgl-3` — the `-3` suffix is the GameCI image version. If unavailable, check [GameCI Docker Hub](https://hub.docker.com/r/unityci/editor/tags) for the correct tag. Mitigation: the build job will fail fast with a clear "image not found" error.
- **Shared runner disk space**: Unity 6 images are ~15GB. GitLab shared runners may not have enough disk. Mitigation: disk cleanup step + the job will fail with a clear disk space error. Long-term fix noted in `deployment.md`: self-hosted runner on Mac mini.
- **License activation method**: GameCI images support multiple activation methods. The `echo "$UNITY_LICENSE"` approach assumes the `.ulf` file content is stored directly in the CI variable. If the runner uses a different method (e.g., serial key), the `before_script` needs adjustment.

### Insights
- `WebGLBuildScript.cs` already reads `BUILD_PATH` env var — no need to pass `-customBuildPath` flag. Setting the env var in the GitLab job `variables:` block is sufficient.
- The deploy job currently deploys from `build/` which is wrong even without this change — it would need `build/WebGL/golf-game/` to match the local build output. This fix is a correctness improvement regardless.

Skip Agents: visual-designer
