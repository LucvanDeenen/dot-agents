<script setup lang="ts">
import { ref, onMounted } from "vue";
import { PencilSquareIcon, PlusIcon, TrashIcon } from "@heroicons/vue/24/outline";
import Page from "@/components/layout/Page.vue";
import {
  createAgent,
  createSkill,
  deleteAgent,
  deleteSkill,
  listAgents,
  listSkills,
  updateAgent,
  updateSkill,
  type Agent,
  type Skill,
} from "@/api/agentPlatform";

const agents = ref<Agent[]>([]);
const skills = ref<Skill[]>([]);
const errorMessage = ref("");

// ── Agent form ──────────────────────────────────────────────────────────────
const showAgentForm = ref(false);
const editingAgentId = ref<string | null>(null);
const agentName = ref("");
const agentDescription = ref("");
const agentSystemPrompt = ref("");
const agentAllowedTools = ref("");
const agentRoutingPattern = ref("task.#");
const agentEnabled = ref(true);
const agentSkillIds = ref<string[]>([]);
const savingAgent = ref(false);

function closeAgentForm() {
  showAgentForm.value = false;
  editingAgentId.value = null;
  agentName.value = "";
  agentDescription.value = "";
  agentSystemPrompt.value = "";
  agentAllowedTools.value = "";
  agentRoutingPattern.value = "task.#";
  agentEnabled.value = true;
  agentSkillIds.value = [];
}

function newAgent() {
  closeAgentForm();
  showAgentForm.value = true;
}

function editAgent(agent: Agent) {
  editingAgentId.value = agent.id;
  agentName.value = agent.name;
  agentDescription.value = agent.description ?? "";
  agentSystemPrompt.value = agent.systemPrompt;
  agentAllowedTools.value = agent.allowedTools.join(" ");
  agentRoutingPattern.value = agent.routingKeyPattern;
  agentEnabled.value = agent.enabled;
  agentSkillIds.value = agent.skills.map((skill) => skill.id);
  showAgentForm.value = true;
}

async function saveAgent() {
  savingAgent.value = true;
  errorMessage.value = "";
  try {
    const request = {
      name: agentName.value.trim(),
      description: agentDescription.value.trim() || null,
      systemPrompt: agentSystemPrompt.value.trim(),
      allowedTools: agentAllowedTools.value.split(/[\s,]+/).filter(Boolean),
      routingKeyPattern: agentRoutingPattern.value.trim() || "task.#",
      enabled: agentEnabled.value,
      skillIds: agentSkillIds.value,
    };
    if (editingAgentId.value) await updateAgent(editingAgentId.value, request);
    else await createAgent(request);
    closeAgentForm();
    await refresh();
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : "Failed to save agent.";
  } finally {
    savingAgent.value = false;
  }
}

async function removeAgent(agent: Agent) {
  if (!window.confirm(`Delete agent "${agent.name}"?`)) return;
  errorMessage.value = "";
  try {
    await deleteAgent(agent.id);
    if (editingAgentId.value === agent.id) closeAgentForm();
    await refresh();
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : "Failed to delete agent.";
  }
}

// ── Skill form ──────────────────────────────────────────────────────────────
const showSkillForm = ref(false);
const editingSkillId = ref<string | null>(null);
const skillName = ref("");
const skillDescription = ref("");
const skillInstructions = ref("");
const savingSkill = ref(false);

function closeSkillForm() {
  showSkillForm.value = false;
  editingSkillId.value = null;
  skillName.value = "";
  skillDescription.value = "";
  skillInstructions.value = "";
}

function newSkill() {
  closeSkillForm();
  showSkillForm.value = true;
}

function editSkill(skill: Skill) {
  editingSkillId.value = skill.id;
  skillName.value = skill.name;
  skillDescription.value = skill.description ?? "";
  skillInstructions.value = skill.instructions;
  showSkillForm.value = true;
}

async function saveSkill() {
  savingSkill.value = true;
  errorMessage.value = "";
  try {
    const request = {
      name: skillName.value.trim(),
      description: skillDescription.value.trim() || null,
      instructions: skillInstructions.value.trim(),
    };
    if (editingSkillId.value) await updateSkill(editingSkillId.value, request);
    else await createSkill(request);
    closeSkillForm();
    await refresh();
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : "Failed to save skill.";
  } finally {
    savingSkill.value = false;
  }
}

async function removeSkill(skill: Skill) {
  if (!window.confirm(`Delete skill "${skill.name}"?`)) return;
  errorMessage.value = "";
  try {
    await deleteSkill(skill.id);
    if (editingSkillId.value === skill.id) closeSkillForm();
    await refresh();
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : "Failed to delete skill.";
  }
}

async function refresh() {
  try {
    [agents.value, skills.value] = await Promise.all([listAgents(), listSkills()]);
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : "Failed to load agents.";
  }
}

onMounted(refresh);
</script>

<template>
  <Page title="Agents" class="min-h-screen">
    <p v-if="errorMessage" class="text-red-400 text-sm mb-4">{{ errorMessage }}</p>

    <div class="grid lg:grid-cols-2 gap-8 max-w-[1200px] items-start">
      <!-- Agents -->
      <div>
        <div class="flex items-center justify-between mb-3">
          <h3 class="!pb-0">Agents</h3>
          <button
            @click="newAgent"
            class="flex items-center gap-1 text-sm text-gray-400 hover:text-yellow-500 transition-colors"
          >
            <PlusIcon class="w-4 h-4" /> New agent
          </button>
        </div>

        <p v-if="agents.length === 0 && !showAgentForm" class="text-gray-500 text-sm">
          No agents yet — without one, tasks fail with "no agent matches".
        </p>

        <ul class="flex flex-col gap-2">
          <li v-for="agent in agents" :key="agent.id" class="group bg-neutral-800 rounded-lg p-3">
            <div class="flex items-center gap-2">
              <span
                class="w-2 h-2 rounded-full shrink-0"
                :class="agent.enabled ? 'bg-green-500' : 'bg-neutral-600'"
                :title="agent.enabled ? 'Enabled' : 'Disabled'"
              />
              <span class="font-medium truncate">{{ agent.name }}</span>
              <code class="text-xs text-gray-500 shrink-0">{{ agent.routingKeyPattern }}</code>
              <span class="flex-1" />
              <span
                class="flex gap-2 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity"
              >
                <button @click="editAgent(agent)" title="Edit" class="text-gray-400 hover:text-white">
                  <PencilSquareIcon class="w-4 h-4" />
                </button>
                <button @click="removeAgent(agent)" title="Delete" class="text-gray-400 hover:text-red-400">
                  <TrashIcon class="w-4 h-4" />
                </button>
              </span>
            </div>
            <p v-if="agent.description" class="text-sm text-gray-400 mt-1 line-clamp-1">
              {{ agent.description }}
            </p>
            <p v-if="agent.skills.length" class="text-xs text-gray-500 mt-1 truncate">
              Skills: {{ agent.skills.map((skill) => skill.name).join(", ") }}
            </p>
          </li>
        </ul>

        <form v-if="showAgentForm" @submit.prevent="saveAgent" class="flex flex-col gap-3 mt-4 bg-neutral-800/50 rounded-lg p-4">
          <h4 class="!pb-0 !text-base">{{ editingAgentId ? "Edit agent" : "New agent" }}</h4>
          <div class="flex gap-3">
            <label class="flex flex-col gap-1 flex-1">
              <span class="text-sm text-gray-400">Name</span>
              <input v-model="agentName" required type="text" placeholder="developer"
                class="bg-neutral-800 border border-neutral-700 rounded p-2 text-sm focus:outline-none focus:border-yellow-500" />
            </label>
            <label class="flex flex-col gap-1 flex-1">
              <span class="text-sm text-gray-400">Routing key pattern</span>
              <input v-model="agentRoutingPattern" required type="text" placeholder="task.#"
                class="bg-neutral-800 border border-neutral-700 rounded p-2 text-sm focus:outline-none focus:border-yellow-500" />
            </label>
          </div>
          <label class="flex flex-col gap-1">
            <span class="text-sm text-gray-400">Description (optional)</span>
            <input v-model="agentDescription" type="text" placeholder="What this agent is for"
              class="bg-neutral-800 border border-neutral-700 rounded p-2 text-sm focus:outline-none focus:border-yellow-500" />
          </label>
          <label class="flex flex-col gap-1">
            <span class="text-sm text-gray-400">System prompt / context</span>
            <textarea v-model="agentSystemPrompt" required rows="4"
              placeholder="Context and instructions injected into every run of this agent"
              class="bg-neutral-800 border border-neutral-700 rounded p-2 text-sm focus:outline-none focus:border-yellow-500" />
          </label>
          <label class="flex flex-col gap-1">
            <span class="text-sm text-gray-400">Allowed tools (space separated, empty = default toolset)</span>
            <input v-model="agentAllowedTools" type="text" placeholder='Bash(git:*) Edit Write WebFetch'
              class="bg-neutral-800 border border-neutral-700 rounded p-2 text-sm focus:outline-none focus:border-yellow-500" />
          </label>
          <div v-if="skills.length" class="flex flex-col gap-1">
            <span class="text-sm text-gray-400">Skills</span>
            <div class="flex flex-wrap gap-3">
              <label v-for="skill in skills" :key="skill.id" class="flex items-center gap-1.5 text-sm">
                <input type="checkbox" :value="skill.id" v-model="agentSkillIds" class="accent-yellow-500" />
                {{ skill.name }}
              </label>
            </div>
          </div>
          <label class="flex items-center gap-2 text-sm">
            <input type="checkbox" v-model="agentEnabled" class="accent-yellow-500" />
            Enabled (picks up tasks)
          </label>
          <div class="flex gap-3">
            <button type="submit" :disabled="savingAgent || !agentName.trim() || !agentSystemPrompt.trim()"
              class="self-start px-4 py-2 rounded bg-yellow-500 text-neutral-900 font-medium disabled:opacity-50 disabled:cursor-not-allowed hover:bg-yellow-400 transition-colors">
              {{ savingAgent ? "Saving..." : editingAgentId ? "Update agent" : "Create agent" }}
            </button>
            <button type="button" @click="closeAgentForm" class="text-sm text-gray-400 hover:text-white">
              Cancel
            </button>
          </div>
        </form>
      </div>

      <!-- Skills -->
      <div>
        <div class="flex items-center justify-between mb-3">
          <h3 class="!pb-0">Skills</h3>
          <button
            @click="newSkill"
            class="flex items-center gap-1 text-sm text-gray-400 hover:text-yellow-500 transition-colors"
          >
            <PlusIcon class="w-4 h-4" /> New skill
          </button>
        </div>

        <p v-if="skills.length === 0 && !showSkillForm" class="text-gray-500 text-sm">
          No skills yet — skills are reusable instruction sets agents can share.
        </p>

        <ul class="flex flex-col gap-2">
          <li v-for="skill in skills" :key="skill.id" class="group bg-neutral-800 rounded-lg p-3">
            <div class="flex items-center gap-2">
              <span class="font-medium truncate">{{ skill.name }}</span>
              <span class="flex-1" />
              <span class="flex gap-2 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
                <button @click="editSkill(skill)" title="Edit" class="text-gray-400 hover:text-white">
                  <PencilSquareIcon class="w-4 h-4" />
                </button>
                <button @click="removeSkill(skill)" title="Delete" class="text-gray-400 hover:text-red-400">
                  <TrashIcon class="w-4 h-4" />
                </button>
              </span>
            </div>
            <p v-if="skill.description" class="text-sm text-gray-400 mt-1 line-clamp-1">
              {{ skill.description }}
            </p>
          </li>
        </ul>

        <form v-if="showSkillForm" @submit.prevent="saveSkill" class="flex flex-col gap-3 mt-4 bg-neutral-800/50 rounded-lg p-4">
          <h4 class="!pb-0 !text-base">{{ editingSkillId ? "Edit skill" : "New skill" }}</h4>
          <label class="flex flex-col gap-1">
            <span class="text-sm text-gray-400">Name</span>
            <input v-model="skillName" required type="text" placeholder="conventional-commits"
              class="bg-neutral-800 border border-neutral-700 rounded p-2 text-sm focus:outline-none focus:border-yellow-500" />
          </label>
          <label class="flex flex-col gap-1">
            <span class="text-sm text-gray-400">Description (optional)</span>
            <input v-model="skillDescription" type="text" placeholder="When should an agent reach for this skill?"
              class="bg-neutral-800 border border-neutral-700 rounded p-2 text-sm focus:outline-none focus:border-yellow-500" />
          </label>
          <label class="flex flex-col gap-1">
            <span class="text-sm text-gray-400">Instructions</span>
            <textarea v-model="skillInstructions" required rows="6"
              placeholder="The skill body: instructions the agent loads when this skill applies"
              class="bg-neutral-800 border border-neutral-700 rounded p-2 text-sm focus:outline-none focus:border-yellow-500" />
          </label>
          <div class="flex gap-3">
            <button type="submit" :disabled="savingSkill || !skillName.trim() || !skillInstructions.trim()"
              class="self-start px-4 py-2 rounded bg-yellow-500 text-neutral-900 font-medium disabled:opacity-50 disabled:cursor-not-allowed hover:bg-yellow-400 transition-colors">
              {{ savingSkill ? "Saving..." : editingSkillId ? "Update skill" : "Create skill" }}
            </button>
            <button type="button" @click="closeSkillForm" class="text-sm text-gray-400 hover:text-white">
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  </Page>
</template>
