<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted } from "vue";
import { TrashIcon } from "@heroicons/vue/24/outline";
import Page from "@/components/layout/Page.vue";
import {
  AgentTaskStatus,
  deleteJob,
  listJobs,
  subscribeToJobStatus,
  type AgentTask,
  type JobStatusChangedEvent,
} from "@/api/agentPlatform";

const props = defineProps<{
  focusTaskId?: string | null;
}>();

const columns: { title: string; statuses: AgentTaskStatus[]; accent: string }[] = [
  { title: "Queued", statuses: [AgentTaskStatus.Pending, AgentTaskStatus.Queued], accent: "border-gray-500" },
  { title: "In progress", statuses: [AgentTaskStatus.Running], accent: "border-yellow-500" },
  { title: "Completed", statuses: [AgentTaskStatus.Completed], accent: "border-green-500" },
  { title: "Failed", statuses: [AgentTaskStatus.Failed], accent: "border-red-500" },
];

const jobs = ref<AgentTask[]>([]);
const selectedJob = ref<AgentTask | null>(null);
const errorMessage = ref("");
let stopStatusSubscription: (() => void) | null = null;

const jobsByColumn = computed(() =>
  columns.map((column) => ({
    ...column,
    jobs: jobs.value.filter((job) => column.statuses.includes(job.status)),
  }))
);

async function refreshJobs() {
  try {
    jobs.value = await listJobs();
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : "Failed to load tasks.";
  }
}

function applyJobStatusUpdate(event: JobStatusChangedEvent) {
  const index = jobs.value.findIndex((job) => job.id === event.taskId);
  if (index === -1) {
    // A task created in another tab/page — pull the full list once.
    void refreshJobs();
    return;
  }

  const updated = {
    ...jobs.value[index],
    status: event.status,
    updatedAt: event.updatedAt,
    agentId: event.agentId ?? jobs.value[index].agentId,
    agentName: event.agentName ?? jobs.value[index].agentName,
    output: event.output ?? jobs.value[index].output,
  };
  jobs.value[index] = updated;
  if (selectedJob.value?.id === updated.id) selectedJob.value = updated;
}

function formatTime(value: string | null): string {
  if (!value) return "";
  return new Date(value).toLocaleString();
}

async function removeJob(job: AgentTask) {
  if (job.status === AgentTaskStatus.Running) return;
  if (!window.confirm("Delete this task?")) return;

  errorMessage.value = "";
  try {
    await deleteJob(job.id);
    jobs.value = jobs.value.filter((candidate) => candidate.id !== job.id);
    if (selectedJob.value?.id === job.id) selectedJob.value = null;
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : "Failed to delete task.";
  }
}

onMounted(async () => {
  await refreshJobs();
  if (props.focusTaskId) {
    // Arriving from the Tasks page: open the freshly submitted task. It may
    // lag the list by a beat right after creation — retry once if needed.
    const focused = jobs.value.find((job) => job.id === props.focusTaskId);
    if (focused) selectedJob.value = focused;
    else {
      await refreshJobs();
      selectedJob.value = jobs.value.find((job) => job.id === props.focusTaskId) ?? null;
    }
  }
  stopStatusSubscription = subscribeToJobStatus(applyJobStatusUpdate);
});

onUnmounted(() => {
  stopStatusSubscription?.();
  stopStatusSubscription = null;
});
</script>

<template>
  <Page title="Board" class="min-h-screen">
    <p v-if="errorMessage" class="text-red-400 text-sm mb-4">{{ errorMessage }}</p>

    <div class="grid md:grid-cols-2 xl:grid-cols-4 gap-4 items-start">
      <div
        v-for="column in jobsByColumn"
        :key="column.title"
        class="bg-neutral-800/50 rounded-lg p-3 border-t-2"
        :class="column.accent"
      >
        <div class="flex items-center justify-between mb-3 px-1">
          <span class="text-sm font-medium text-gray-300">{{ column.title }}</span>
          <span class="text-xs text-gray-500">{{ column.jobs.length }}</span>
        </div>
        <p v-if="column.jobs.length === 0" class="text-xs text-gray-600 px-1 pb-1">Empty</p>
        <ul class="flex flex-col gap-2">
          <li
            v-for="job in column.jobs"
            :key="job.id"
            @click="selectedJob = job"
            class="group bg-neutral-800 rounded p-3 cursor-pointer hover:bg-neutral-700/80 transition-colors"
          >
            <div class="flex items-start justify-between gap-2">
              <p class="text-sm line-clamp-3">{{ job.instruction }}</p>
              <button
                v-if="job.status !== AgentTaskStatus.Running"
                @click.stop="removeJob(job)"
                title="Delete task"
                class="opacity-0 group-hover:opacity-100 shrink-0 text-gray-500 hover:text-red-400 transition-opacity"
              >
                <TrashIcon class="w-4 h-4" />
              </button>
            </div>
            <div class="flex items-center justify-between mt-2 gap-2">
              <span class="text-xs text-yellow-500/80 truncate">{{ job.agentName ?? "unassigned" }}</span>
              <span class="text-xs text-gray-500 shrink-0">{{ formatTime(job.updatedAt ?? job.createdAt) }}</span>
            </div>
          </li>
        </ul>
      </div>
    </div>

    <!-- Task detail overlay -->
    <div
      v-if="selectedJob"
      @click.self="selectedJob = null"
      class="fixed inset-0 bg-black/60 flex items-center justify-center p-6 z-50"
    >
      <div class="bg-neutral-800 rounded-lg max-w-[700px] w-full max-h-[80vh] overflow-y-auto p-5">
        <div class="flex items-start justify-between gap-3 mb-3">
          <h4 class="!pb-0">Task detail</h4>
          <div class="flex items-center gap-4 text-sm">
            <button
              v-if="selectedJob.status !== AgentTaskStatus.Running"
              @click="removeJob(selectedJob)"
              class="text-gray-400 hover:text-red-400"
            >
              Delete
            </button>
            <button @click="selectedJob = null" class="text-gray-400 hover:text-white">Close</button>
          </div>
        </div>
        <dl class="text-sm flex flex-col gap-2">
          <div>
            <dt class="text-gray-400">Instruction</dt>
            <dd class="whitespace-pre-wrap">{{ selectedJob.instruction }}</dd>
          </div>
          <div class="flex gap-6 flex-wrap">
            <div>
              <dt class="text-gray-400">Agent</dt>
              <dd>{{ selectedJob.agentName ?? "unassigned" }}</dd>
            </div>
            <div>
              <dt class="text-gray-400">Routing key</dt>
              <dd>{{ selectedJob.routingKey }}</dd>
            </div>
            <div v-if="selectedJob.repoUrl">
              <dt class="text-gray-400">Repo</dt>
              <dd>{{ selectedJob.repoUrl }}<span v-if="selectedJob.branch"> ({{ selectedJob.branch }})</span></dd>
            </div>
          </div>
          <div v-if="selectedJob.output">
            <dt class="text-gray-400">Output</dt>
            <dd>
              <pre class="whitespace-pre-wrap text-xs bg-neutral-900 rounded p-3 mt-1">{{ selectedJob.output }}</pre>
            </dd>
          </div>
        </dl>
      </div>
    </div>
  </Page>
</template>
