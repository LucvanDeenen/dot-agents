<script setup lang="ts">
const props = defineProps<{
  selected?: boolean;
  project?: any;
}>();

const project = props.project ?? undefined;
</script>

<template>
  <div
    class="content"
    :class="{
      'border-animate border-animate-active': props.selected,
      'border-animate': !props.selected,
      'content-image scroll-snap-center relative overflow-hidden': project,
    }"
  >
    <div v-if="project">
      <!-- Content -->
      <div class="absolute top-0 right-0 mr-2 mt-4">
        <!-- GitHub Icon -->
        <a
          v-if="project.links?.github"
          :href="project.links?.github"
          target="_blank"
          rel="noopener noreferrer"
          class="inline-block mx-1 sm:mx-2"
          title="View on GitHub"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="w-5 sm:w-7 text-gray-400 hover:text-yellow-500 transition-colors"
            viewBox="0 0 24 24"
            fill="currentColor"
          >
            <path
              d="M12 2C6.477 2 2 6.484 2 12.021c0 4.428 2.865 8.184 6.839 9.504.5.092.682-.217.682-.483 0-.237-.009-.868-.014-1.703-2.782.605-3.369-1.342-3.369-1.342-.454-1.154-1.11-1.462-1.11-1.462-.908-.62.069-.608.069-.608 1.004.07 1.532 1.032 1.532 1.032.892 1.53 2.341 1.088 2.91.832.091-.647.35-1.088.636-1.339-2.221-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.254-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.025A9.564 9.564 0 0 1 12 6.844c.85.004 1.705.115 2.504.337 1.909-1.295 2.748-1.025 2.748-1.025.546 1.378.202 2.396.1 2.65.64.7 1.028 1.595 1.028 2.688 0 3.847-2.337 4.695-4.566 4.944.359.309.678.919.678 1.852 0 1.336-.012 2.415-.012 2.744 0 .268.18.579.688.481C19.138 20.2 22 16.447 22 12.021 22 6.484 17.523 2 12 2z"
            />
          </svg>
        </a>

        <!-- Demo Icon -->
        <a
          v-if="project.links?.demo"
          :href="project.links?.demo"
          target="_blank"
          rel="noopener noreferrer"
          class="inline-block mx-1"
          title="View Demo"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="w-5 sm:w-7 text-gray-400 hover:text-yellow-500 transition-colors"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            stroke-width="2"
          >
            <rect
              x="3"
              y="4"
              width="18"
              height="14"
              rx="2"
              stroke="currentColor"
              stroke-width="2"
              fill="none"
            />
            <path
              d="M8 21h8"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
            />
            <path
              d="M12 17v4"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
            />
            <polygon points="10,8 16,12 10,16" fill="currentColor" />
          </svg>
        </a>
      </div>

      <!-- Title -->
      <h3 class="mb-0 text-yellow-500">{{ project.title }}</h3>
      <p>{{ project.subtitle }}</p>

      <!-- Details -->
      <div class="pb-2" v-for="line in project.description">- {{ line }}</div>

      <!-- Technologies -->
      <div class="mt-3">
        <div class="flex flex-wrap gap-2">
          <span
            v-for="technology in project.technologies"
            :key="technology"
            class="chip"
          >
            {{ technology }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="css" scoped>
.content {
  @apply min-h-[425px] max-h-[425px] sm:min-h-[500px] sm:max-h-[500px] min-w-[85vw] sm:min-w-[500px];
  @apply rounded px-6 pt-3 bg-neutral-800 select-none;
}

.content-image {
  @apply bg-cover bg-center;
}

.border-animate::before {
  @apply bg-yellow-500;
  content: "";
  position: absolute;
  left: 0;
  top: 50%;
  transform: translateY(-50%);
  width: 4px;
  height: 0%;
  transition: height 300ms ease;
}

.border-animate-active::before {
  height: 100%;
}
</style>
