<script setup lang="ts">
import { ref, provide, watch, nextTick, onMounted } from "vue";
import ExperiencePage from "@/pages/ExperiencePage.vue";
import ProjectPage from "@/pages/ProjectPage.vue";
import ContactPage from "@/pages/ContactPage.vue";
import AboutPage from "@/pages/AboutPage.vue";
import HomePage from "@/pages/HomePage.vue";

import { ChevronDoubleUpIcon } from "@heroicons/vue/24/solid";

const activeSection = ref("home-page");
const targetSection = ref();
const scrolling = ref(false);
const loadedSections = ref<Record<string, boolean>>({});
const updateSection = (sectionId: string) => {
  targetSection.value = sectionId;
};

onMounted(() => {
  const sections = document.querySelectorAll("section");
  sections.forEach((section) => {
    loadedSections.value[section.id] = false;
  });

  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          activeSection.value = entry.target.id;
          loadedSections.value[entry.target.id] = true;
        }
      });
    },
    { threshold: 0.05 }
  );
  sections.forEach((section) => observer.observe(section));
});

watch(targetSection, async (target) => {
  if (!target) return;
  await nextTick();

  let el: HTMLElement | null = document.querySelector(`#${target}`);
  if (el) {
    const offset = window.innerHeight / 8;
    const top = el.getBoundingClientRect().top + window.scrollY - offset;
    window.scrollTo({ top, behavior: "smooth" });
    scrolling.value = true;
    activeSection.value = target;
    targetSection.value = null;

    setTimeout(() => {
      scrolling.value = false;
    }, 500);
  }
});
provide("app", { updateSection });
</script>

<template>
  <div class="overflow-x-hidden">
    <div
      class="fixed bottom-5 right-5 sm:right-[10%] sm:bottom-10 z-50 transition-all duration-700 opacity-0 -translate-x-10"
      :class="{ 'opacity-100 translate-x-0': activeSection !== 'home-page' }"
      @click="updateSection('home-page')"
    >
      <button
        class="button-icon hover:border-yellow-500 hover:text-yellow-500"
        v-if="!scrolling"
      >
        <ChevronDoubleUpIcon class="w-5 sm:w-7" />
      </button>
    </div>
    <HomePage id="home-page" class="mb-12" />
    <AboutPage
      id="about-page"
      class="transition-all duration-700 ease-in-out"
      :class="
        loadedSections['about-page']
          ? 'opacity-100 translate-x-0'
          : 'opacity-0 -translate-x-10'
      "
    />
    <ProjectPage
      id="project-page"
      class="transition-all duration-700 ease-in-out"
      :class="
        loadedSections['project-page']
          ? 'opacity-100 translate-x-0'
          : 'opacity-0 translate-x-10'
      "
    />
    <ExperiencePage
      id="experience-page"
      class="transition-all duration-700 ease-in-out"
      :class="
        loadedSections['experience-page']
          ? 'opacity-100 translate-x-0'
          : 'opacity-0 translate-x-10'
      "
    />
    <ContactPage
      id="contact-page"
      class="transition-all duration-700 ease-in-out"
      :class="
        loadedSections['contact-page']
          ? 'opacity-100 translate-x-0'
          : 'opacity-0 -translate-x-10'
      "
    />
  </div>
</template>
