<script setup lang="ts">
import { ref, onMounted } from "vue";
import Page from "@/components/layout/Page.vue";
import {
  AgentTaskStatus,
  createJob,
  listJobs,
  JobNotQueuedError,
  type AgentTask,
} from "@/api/agentPlatform";

const statusLabel: Record<AgentTaskStatus, string> = {
  [AgentTaskStatus.Pending]: "Pending",
  [AgentTaskStatus.Queued]: "Queued",
  [AgentTaskStatus.Running]: "Running",
  [AgentTaskStatus.Completed]: "Completed",
  [AgentTaskStatus.Failed]: "Failed",
};

const prompt = ref("");
const repoUrl = ref("");
const branch = ref("");
const submitting = ref(false);
const errorMessage = ref("");
const warningMessage = ref("");

const jobs = ref<AgentTask[]>([]);
const loadingJobs = ref(false);

async function refreshJobs() {
  loadingJobs.value = true;
  try {
    jobs.value = await listJobs();
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : "Failed to load jobs.";
  } finally {
    loadingJobs.value = false;
  }
}

async function submitJob() {
  if (!prompt.value.trim()) return;

  submitting.value = true;
  errorMessage.value = "";
  warningMessage.value = "";
  try {
    await createJob({
      prompt: prompt.value.trim(),
      repoUrl: repoUrl.value.trim() || null,
      branch: branch.value.trim() || null,
    });
    prompt.value = "";
    repoUrl.value = "";
    branch.value = "";
    await refreshJobs();
  } catch (err) {
    if (err instanceof JobNotQueuedError) {
      warningMessage.value = err.message;
      await refreshJobs();
    } else {
      errorMessage.value = err instanceof Error ? err.message : "Failed to create job.";
    }
  } finally {
    submitting.value = false;
  }
}

onMounted(refreshJobs);
</script>

<template>
  <Page title="Agent Platform" class="min-h-screen">
    <form @submit.prevent="submitJob" class="flex flex-col gap-3 max-w-[700px]">
      <label class="flex flex-col gap-1">
        <span class="text-sm text-gray-400">Prompt</span>
        <textarea
          v-model="prompt"
          required
          rows="3"
          placeholder="Describe the task for the agent to work on"
          class="bg-neutral-800 border border-neutral-700 rounded p-2 text-sm focus:outline-none focus:border-yellow-500"
        />
      </label>
      <div class="flex gap-3">
        <label class="flex flex-col gap-1 flex-1">
          <span class="text-sm text-gray-400">Repo URL (optional)</span>
          <input
            v-model="repoUrl"
            type="text"
            placeholder="https://github.com/user/repo"
            class="bg-neutral-800 border border-neutral-700 rounded p-2 text-sm focus:outline-none focus:border-yellow-500"
          />
        </label>
        <label class="flex flex-col gap-1 flex-1">
          <span class="text-sm text-gray-400">Branch (optional)</span>
          <input
            v-model="branch"
            type="text"
            placeholder="main"
            class="bg-neutral-800 border border-neutral-700 rounded p-2 text-sm focus:outline-none focus:border-yellow-500"
          />
        </label>
      </div>
      <button
        type="submit"
        :disabled="submitting || !prompt.trim()"
        class="self-start px-4 py-2 rounded bg-yellow-500 text-neutral-900 font-medium disabled:opacity-50 disabled:cursor-not-allowed hover:bg-yellow-400 transition-colors"
      >
        {{ submitting ? "Submitting..." : "Submit job" }}
      </button>
      <p v-if="errorMessage" class="text-red-400 text-sm">{{ errorMessage }}</p>
      <p v-if="warningMessage" class="text-yellow-500 text-sm">{{ warningMessage }}</p>
    </form>

    <div class="mt-8 max-w-[700px]">
      <div class="flex items-center justify-between mb-2">
        <h3>Recent jobs</h3>
        <button
          @click="refreshJobs"
          :disabled="loadingJobs"
          class="text-sm text-gray-400 hover:text-white transition-colors disabled:opacity-50"
        >
          {{ loadingJobs ? "Refreshing..." : "Refresh" }}
        </button>
      </div>
      <p v-if="!loadingJobs && jobs.length === 0" class="text-gray-500 text-sm">No jobs yet.</p>
      <ul class="flex flex-col gap-2">
        <li
          v-for="job in jobs"
          :key="job.id"
          class="bg-neutral-800 rounded p-3 flex items-center justify-between gap-3"
        >
          <span class="text-sm truncate">{{ job.instruction }}</span>
          <span class="chip shrink-0">{{ statusLabel[job.status] }}</span>
        </li>
      </ul>
    </div>
  </Page>
</template>
