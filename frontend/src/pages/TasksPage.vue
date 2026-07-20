<script setup lang="ts">
import { ref } from "vue";
import { PaperAirplaneIcon } from "@heroicons/vue/24/outline";
import { createJob, JobNotQueuedError } from "@/api/agentPlatform";

const emit = defineEmits<{
  submitted: [taskId: string];
}>();

const prompt = ref("");
const submitting = ref(false);
const errorMessage = ref("");
const warningMessage = ref("");

async function submitJob() {
  if (!prompt.value.trim() || submitting.value) return;

  submitting.value = true;
  errorMessage.value = "";
  warningMessage.value = "";
  try {
    const task = await createJob({ prompt: prompt.value.trim() });
    prompt.value = "";
    emit("submitted", task.id);
  } catch (err) {
    if (err instanceof JobNotQueuedError) {
      // Persisted but not queued (broker down) — the task exists, but there
      // is no id in the 202 payload to focus on the board.
      warningMessage.value = err.message;
    } else {
      errorMessage.value = err instanceof Error ? err.message : "Failed to create job.";
    }
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <section class="min-h-screen flex items-center justify-center p-6">
    <div class="w-full max-w-[640px] flex flex-col gap-4 -mt-24">
      <div class="text-center">
        <h2 class="!text-2xl font-bold">What needs doing?</h2>
        <p class="text-sm text-gray-500 mt-1">Describe a task and an agent will pick it up.</p>
      </div>
      <form @submit.prevent="submitJob" class="relative">
        <textarea
          v-model="prompt"
          required
          rows="4"
          autofocus
          placeholder="Describe the task..."
          @keydown.enter.exact.prevent="submitJob"
          class="w-full bg-neutral-800 border border-neutral-700 rounded-xl p-4 pb-12 text-sm resize-none shadow-lg focus:outline-none focus:border-yellow-500"
        />
        <button
          type="submit"
          :disabled="submitting || !prompt.trim()"
          title="Submit task (Enter)"
          class="absolute bottom-3.5 right-3 p-2 rounded-full text-gray-400 hover:text-neutral-900 hover:bg-yellow-500 disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-transparent disabled:hover:text-gray-400 transition-colors"
        >
          <PaperAirplaneIcon class="w-5 h-5" :class="{ 'animate-pulse': submitting }" />
        </button>
      </form>
      <p v-if="errorMessage" class="text-red-400 text-sm text-center">{{ errorMessage }}</p>
      <p v-if="warningMessage" class="text-yellow-500 text-sm text-center">{{ warningMessage }}</p>
    </div>
  </section>
</template>
