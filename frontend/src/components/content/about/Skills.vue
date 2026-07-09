<script setup lang="ts">
import Content from "@/components/layout/Content.vue";
import persona from "@/assets/persona.json";
</script>

<template>
  <Content id-ref="skills">
    <h3 class="text-gray-500">// Technical Skills</h3>
    <p class="mb-3 leading-relaxed">
      Each skill is rated on a scale from 1 to 5, indicating proficiency and
      confidence with the respective language or technology.
    </p>
    <div class="grid gap-8 md:grid-cols-2 lg:grid-cols-3">
      <div
        v-for="category in persona.skills"
        :key="category.name"
        class="project-card border-l-4 border-yellow-500"
      >
        <h4 class="text-yellow-500">
          {{ category.name }}
        </h4>
        <div class="space-y-4">
          <div
            v-for="skill in category.skills"
            :key="skill.name"
            class="space-y-2"
          >
            <div class="flex justify-between text-sm">
              <span class="text-gray-300">{{ skill.name }}</span>
              <span class="text-gray-400">{{ skill.level }}/5</span>
            </div>
            <div class="h-2 bg-gray-600 rounded-full overflow-hidden">
              <div
                class="h-full bg-gray-300 rounded-full transition-all duration-300"
                :style="{ width: `${(skill.level / 5) * 100}%` }"
              ></div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </Content>
</template>
