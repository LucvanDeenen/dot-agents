export enum AgentTaskStatus {
  Pending = 0,
  Queued = 1,
  Running = 2,
  Completed = 3,
  Failed = 4,
}

export interface AgentTask {
  id: string;
  routingKey: string;
  instruction: string;
  status: AgentTaskStatus;
  createdAt: string;
  updatedAt: string | null;
  agentId: string | null;
  agentName: string | null;
  repoUrl: string | null;
  branch: string | null;
  output: string | null;
}

export interface CreateJobRequest {
  prompt: string;
  repoUrl?: string | null;
  branch?: string | null;
  agentId?: string | null;
  routingKey?: string | null;
}

export interface JobStatusChangedEvent {
  taskId: string;
  status: AgentTaskStatus;
  updatedAt: string | null;
  agentId: string | null;
  agentName: string | null;
  output: string | null;
}

export interface Skill {
  id: string;
  name: string;
  description: string | null;
  instructions: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpsertSkillRequest {
  name: string;
  description?: string | null;
  instructions: string;
}

export interface Agent {
  id: string;
  name: string;
  description: string | null;
  systemPrompt: string;
  allowedTools: string[];
  routingKeyPattern: string;
  enabled: boolean;
  createdAt: string;
  updatedAt: string | null;
  skills: Skill[];
}

export interface UpsertAgentRequest {
  name: string;
  description?: string | null;
  systemPrompt: string;
  allowedTools?: string[] | null;
  routingKeyPattern: string;
  enabled: boolean;
  skillIds?: string[] | null;
}

export interface ProblemDetails {
  title?: string | null;
  detail?: string | null;
  status?: number | null;
}

// Thrown when a job was persisted but couldn't be published to the task
// queue (HTTP 202). The task exists in a Pending state, so callers should
// still refresh their job list rather than treat this as a hard failure.
export class JobNotQueuedError extends Error {}

async function problemMessage(response: Response): Promise<string> {
  const problem = (await response.json().catch(() => null)) as ProblemDetails | null;
  return problem?.detail ?? problem?.title ?? `Request failed with status ${response.status}`;
}

async function requestJson<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init);
  if (!response.ok) throw new Error(await problemMessage(response));
  return response.json();
}

function jsonBody(body: unknown, method = "POST"): RequestInit {
  return {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  };
}

// ── Jobs ────────────────────────────────────────────────────────────────────

export function listJobs(): Promise<AgentTask[]> {
  return requestJson("/jobs");
}

export async function createJob(request: CreateJobRequest): Promise<AgentTask> {
  const response = await fetch("/jobs", jsonBody(request));
  if (response.status === 202) throw new JobNotQueuedError(await problemMessage(response));
  if (!response.ok) throw new Error(await problemMessage(response));
  return response.json();
}

export async function deleteJob(id: string): Promise<void> {
  const response = await fetch(`/jobs/${id}`, { method: "DELETE" });
  if (!response.ok) throw new Error(await problemMessage(response));
}

export function subscribeToJobStatus(onStatusChanged: (event: JobStatusChangedEvent) => void): () => void {
  const source = new EventSource("/jobs/events");

  source.addEventListener("job-status", (rawEvent) => {
    const event = rawEvent as MessageEvent<string>;
    try {
      onStatusChanged(JSON.parse(event.data) as JobStatusChangedEvent);
    } catch {
      // Ignore malformed events and keep the stream alive.
    }
  });

  return () => source.close();
}

// ── Agents ──────────────────────────────────────────────────────────────────

export function listAgents(): Promise<Agent[]> {
  return requestJson("/agents");
}

export function createAgent(request: UpsertAgentRequest): Promise<Agent> {
  return requestJson("/agents", jsonBody(request));
}

export function updateAgent(id: string, request: UpsertAgentRequest): Promise<Agent> {
  return requestJson(`/agents/${id}`, jsonBody(request, "PUT"));
}

export async function deleteAgent(id: string): Promise<void> {
  const response = await fetch(`/agents/${id}`, { method: "DELETE" });
  if (!response.ok) throw new Error(await problemMessage(response));
}

// ── Skills ──────────────────────────────────────────────────────────────────

export function listSkills(): Promise<Skill[]> {
  return requestJson("/skills");
}

export function createSkill(request: UpsertSkillRequest): Promise<Skill> {
  return requestJson("/skills", jsonBody(request));
}

export function updateSkill(id: string, request: UpsertSkillRequest): Promise<Skill> {
  return requestJson(`/skills/${id}`, jsonBody(request, "PUT"));
}

export async function deleteSkill(id: string): Promise<void> {
  const response = await fetch(`/skills/${id}`, { method: "DELETE" });
  if (!response.ok) throw new Error(await problemMessage(response));
}
