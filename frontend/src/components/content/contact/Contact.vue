<script setup lang="ts">
import contactInfo from "@/assets/persona.json";
import Content from "@/components/layout/Content.vue";
</script>

<template>
  <Content id-ref="contact" class="project-card">
    <div class="space-y-8">
      <div>
        <h3>Let's Connect!</h3>
        <p class="text-gray-400">
          I'm always open to discussing new projects, creative ideas, or
          opportunities to be part of your vision.
        </p>
      </div>

      <div class="space-y-6">
        <div class="flex items-center gap-3">
          <svg
            class="w-6 h-6 text-gray-400"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
            />
          </svg>
          <a
            :href="'mailto:' + contactInfo.email"
            class="text-gray-400 hover:text-white transition-colors"
          >
            <b class="text-yellow-500">Personal</b> {{ contactInfo.email }}
          </a>
        </div>

        <div class="space-y-6">
          <div class="flex items-center gap-3">
            <svg
              class="w-6 h-6 text-gray-600 dark:text-gray-400"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
              />
            </svg>
            <a
              :href="'mailto:' + contactInfo.workEmail"
              class="text-gray-400 hover:text-white transition-colors"
            >
              <b class="text-yellow-500">Work</b> {{ contactInfo.workEmail }}
            </a>
          </div>
        </div>

        <div class="flex items-center gap-3">
          <svg
            class="w-6 h-6 text-gray-400"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"
            />
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"
            />
          </svg>
          <span class="text-gray-400 cursor-default"
            ><b class="text-yellow-500">{{ contactInfo.employer }}</b
            >, {{ contactInfo.location }}</span
          >
        </div>
      </div>
    </div>
  </Content>
</template>
