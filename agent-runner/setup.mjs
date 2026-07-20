// Materializes the RUN_CONFIG payload (base64 JSON set by DockerAgentRunner)
// into the workspace: the completer subagent, the agent's skills, and the
// guide prompt / session flags that entrypoint.sh feeds to `claude`.
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const raw = process.env.RUN_CONFIG;
if (!raw) {
  console.error("RUN_CONFIG is not set");
  process.exit(1);
}

const config = JSON.parse(Buffer.from(raw, "base64").toString("utf8"));
const home = process.env.HOME ?? "/home/agent";
const workspace = join(home, "workspace");
const claudeDir = join(workspace, ".claude");

const slug = (name) =>
  name.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "") || "skill";

// ── Skills → .claude/skills/<slug>/SKILL.md ─────────────────────────────────
const skills = config.skills ?? [];
for (const skill of skills) {
  const dir = join(claudeDir, "skills", slug(skill.name));
  mkdirSync(dir, { recursive: true });
  writeFileSync(
    join(dir, "SKILL.md"),
    [
      "---",
      `name: ${slug(skill.name)}`,
      `description: ${(skill.description ?? skill.name).replace(/\n/g, " ")}`,
      "---",
      "",
      skill.instructions,
      "",
    ].join("\n")
  );
}

// ── Completer subagent → .claude/agents/completer.md ────────────────────────
const agentsDir = join(claudeDir, "agents");
mkdirSync(agentsDir, { recursive: true });
const skillList = skills.length
  ? `You have these skills available; load the relevant ones before working:\n${skills
      .map((s) => `- ${slug(s.name)}: ${s.description ?? ""}`)
      .join("\n")}`
  : "";
writeFileSync(
  join(agentsDir, "completer.md"),
  [
    "---",
    "name: completer",
    "description: Implementation specialist that completes the concrete work for this task. Use for all hands-on execution.",
    "---",
    "",
    "You are the completer: the implementation half of a two-agent team.",
    "You receive a scoped assignment from the guide. Execute it fully and",
    "report back what you did, what you verified, and anything left open.",
    "",
    config.systemPrompt ?? "",
    "",
    skillList,
    "",
  ].join("\n")
);

// ── Guide prompt ────────────────────────────────────────────────────────────
const repoNote = config.repoUrl
  ? `The task concerns the repository ${config.repoUrl}` +
    (config.branch ? ` (branch ${config.branch})` : "") +
    ". It has been cloned into the current directory if it was reachable; otherwise clone it yourself or work from scratch."
  : "There is no associated repository; work inside the current directory.";

const guidePrompt = [
  `You are the guide of a two-agent team working on behalf of the agent "${config.agentName}".`,
  "Your role: understand the task, break it into assignments, delegate the",
  "hands-on execution to the 'completer' subagent (via the Task tool), review",
  "what it produces, and iterate until the task is genuinely done.",
  "Do not do the implementation work yourself — guide and verify.",
  "",
  repoNote,
  "",
  "# Task",
  config.instruction,
  "",
  "When finished, end with a concise final report: what was done, how it was",
  "verified, and anything the requester should follow up on. If the task",
  "cannot be completed, say why and exit with a non-zero status.",
].join("\n");

writeFileSync(join(home, "guide-prompt.txt"), guidePrompt);
writeFileSync(join(home, "system-prompt.txt"), config.systemPrompt ?? "");
writeFileSync(join(home, "allowed-tools.txt"), (config.allowedTools ?? []).join(" "));
writeFileSync(join(home, "repo-url.txt"), config.repoUrl ?? "");
writeFileSync(join(home, "repo-branch.txt"), config.branch ?? "");

console.error(`setup: workspace ready (${skills.length} skill(s))`);
