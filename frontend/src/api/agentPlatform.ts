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
}

export interface CreateJobRequest {
  prompt: string;
  repoUrl?: string | null;
  branch?: string | null;
}

export interface JobStatusChangedEvent {
  taskId: string;
  status: AgentTaskStatus;
  updatedAt: string | null;
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

export async function listJobs(): Promise<AgentTask[]> {
  const response = await fetch("/jobs");
  if (!response.ok) throw new Error(await problemMessage(response));
  return response.json();
}

export async function createJob(request: CreateJobRequest): Promise<AgentTask> {
  const response = await fetch("/jobs", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  if (response.status === 202) throw new JobNotQueuedError(await problemMessage(response));
  if (!response.ok) throw new Error(await problemMessage(response));
  return response.json();
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
