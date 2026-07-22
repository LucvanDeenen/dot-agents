<script setup lang="ts">
import { ref } from "vue";
import { QueueListIcon, UserGroupIcon, ViewColumnsIcon } from "@heroicons/vue/24/outline";
import TasksPage from "@/pages/TasksPage.vue";
import AgentsPage from "@/pages/AgentsPage.vue";
import BoardPage from "@/pages/BoardPage.vue";

const navItems = [
  { key: "tasks", label: "Tasks", icon: QueueListIcon },
  { key: "agents", label: "Agents", icon: UserGroupIcon },
  { key: "board", label: "Board", icon: ViewColumnsIcon },
];

const activeKey = ref("tasks");
const focusTaskId = ref<string | null>(null);

function navigate(key: string) {
  if (key !== "board") focusTaskId.value = null;
  activeKey.value = key;
}

// After submitting a task, land on the board with that task's detail open.
function openBoardWithTask(taskId: string) {
  focusTaskId.value = taskId;
  activeKey.value = "board";
}
</script>

<template>
  <div class="flex min-h-screen">
    <aside class="w-52 shrink-0 border-r border-neutral-800 p-3 flex flex-col gap-1">
      <div class="px-3 py-2 mb-3 text-lg font-bold text-yellow-500 tracking-wide">dot-agents</div>
      <button
        v-for="item in navItems"
        :key="item.key"
        @click="navigate(item.key)"
        class="flex items-center gap-3 px-3 py-2 rounded text-sm transition-colors text-left"
        :class="
          activeKey === item.key
            ? 'bg-neutral-800 text-white'
            : 'text-gray-400 hover:text-white hover:bg-neutral-800/50'
        "
      >
        <component :is="item.icon" class="w-5 h-5 shrink-0" />
        {{ item.label }}
      </button>
    </aside>
    <main class="flex-1 min-w-0">
      <TasksPage v-if="activeKey === 'tasks'" @submitted="openBoardWithTask" />
      <AgentsPage v-else-if="activeKey === 'agents'" />
      <BoardPage v-else :focus-task-id="focusTaskId" />
    </main>
  </div>
</template>
