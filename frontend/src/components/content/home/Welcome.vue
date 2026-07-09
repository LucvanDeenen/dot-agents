<script setup lang="ts">
import { ref, onMounted, onUnmounted, Ref, computed } from "vue";
import { ChevronDoubleRightIcon } from "@heroicons/vue/24/outline";

import Content from "@/components/layout/Content.vue";
import CodeBlock from "@/components/shared/CodeBlock.vue";

import projects from "@/assets/projects.json";
import content from "@/assets/content.json";
import { fetchUser, getUser } from "auth-client";
import { User } from "auth-client/dist/types/incoming";

type Title = {
  domain: string;
  summary: string;
  tld: string | null;
  href: string | null;
};

const textArray: Array<Title> = projects.map((p: any) => ({
  domain: p.domain,
  summary: p.summary,
  tld: p.tld !== undefined ? p.tld : null,
  href: p.href !== undefined ? p.href : null,
}));
const block = content.sections;
const greetings = content.greetings;

const letters: String = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
const currentDescription = ref("");
const textIndex = ref(0);
const user: Ref<User | undefined> = ref(undefined);
const userComputed = computed(() => user.value);
const greeting = ref("");

let interval: ReturnType<typeof setInterval> | null = null;
let animation: ReturnType<typeof setInterval> | null = null;
const animateText = (title: HTMLElement, description: HTMLElement) => {
  if (!title || !description) return;

  let iteration = 0;
  animation = setInterval(() => {
    title.innerText = title.innerText
      .split("")
      .map((letter, index) => {
        if (index < iteration) return textArray[textIndex.value].domain[index];
        if (
          letter === " " ||
          letter === "_" ||
          letter === "." ||
          letter === "-"
        )
          return letter;
        return letters[Math.floor(Math.random() * letters.length)];
      })
      .join("");

    if (iteration > textArray[textIndex.value].domain.length) {
      clearInterval(animation!);
      description.classList.add("active");
      currentDescription.value = textArray[textIndex.value].summary;
    }

    iteration += 1 / 3;
  }, 50);
};

onMounted(async () => {
  const title = document.getElementById("title");
  const description = document.getElementById("description");

  animateText(title!, description!);
  interval = setInterval(() => {
    description?.classList.remove("active");
    textIndex.value = (textIndex.value + 1) % textArray.length;
    animateText(title!, description!);
  }, 10000);

  const storedUser = getUser();
  storedUser ? (user.value = storedUser) : (user.value = await fetchUser());

  greeting.value = greetings[Math.floor(Math.random() * greetings.length)];
});

onUnmounted(() => {
  if (interval) clearInterval(interval);
  if (animation) clearInterval(animation);
});
</script>

<template>
  <Content
    id-ref="welcome"
    class="w-[80%] max-w-[750px] mx-auto h-screen flex flex-col justify-center"
  >
    <!-- Title -->
    <a
      class="flex min-w-[200px] transition-all"
      :target="
        textArray[textIndex].tld !== null
          ? textArray[textIndex].domain !== 'dedeen'
            ? '_blank'
            : '_self'
          : undefined
      "
      :href="
        textArray[textIndex].href ??
        `https://${textArray[textIndex].domain}.${textArray[textIndex].tld}`
      "
    >
      <ChevronDoubleRightIcon class="sm:w-10 w-7 mr-1 text-yellow-500" />
      <h1 class="text-slate-100 font-bold uppercase bg-clip-text relative">
        <span id="title" class="link hover:cursor-pointer">
          {{ textArray[textIndex].domain }}
        </span>
        <span v-if="textArray[textIndex].tld !== null"
          >.<span class="text-yellow-500">{{ textArray[textIndex].tld }}</span>
        </span>
      </h1>
    </a>

    <!-- Description -->
    <p class="text-gray-500 ml-1 sm:text-base text-xs">
      // My project
      <span id="description"> > {{ currentDescription }} </span>
    </p>

    <!-- Links -->
    <div class="w-full mt-2 sm:text-base text-xs">
      <CodeBlock :block="block" />
    </div>

    <!-- Quick links -->
    <div class="flex justify-between mt-2">
      <p
        class="opacity-0 transition-opacity duration-500 ml-3"
        :class="{ 'opacity-100': userComputed }"
      >
        {{ greeting }} {{ userComputed?.username }}!
      </p>
      <div class="flex gap-3 text-gray-600">
        <a
          href="https://github.com/LucvanDeenen"
          target="_blank"
          class="hover:text-white transition-colors duration-200"
        >
          <svg class="w-5" viewBox="0 0 24 24" fill="currentColor">
            <path
              d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"
            />
          </svg>
        </a>
        <a
          href="https://www.linkedin.com/in/luc-van-deenen-561824194"
          target="_blank"
          class="hover:text-white transition-colors duration-200"
        >
          <svg class="w-5" viewBox="0 0 24 24" fill="currentColor">
            <path
              d="M19 0h-14c-2.761 0-5 2.239-5 5v14c0 2.761 2.239 5 5 5h14c2.762 0 5-2.239 5-5v-14c0-2.761-2.238-5-5-5zm-11 19h-3v-11h3v11zm-1.5-12.268c-.966 0-1.75-.79-1.75-1.764s.784-1.764 1.75-1.764 1.75.79 1.75 1.764-.783 1.764-1.75 1.764zm13.5 12.268h-3v-5.604c0-3.368-4-3.113-4 0v5.604h-3v-11h3v1.765c1.396-2.586 7-2.777 7 2.476v6.759z"
            />
          </svg>
        </a>
      </div>
    </div>
  </Content>
</template>

<style scoped>
.link:after {
  @apply content-[''] text-yellow-500 absolute bottom-0 left-0 right-0 h-0.5 bg-current transition-transform duration-100 scale-x-0 origin-left;
}
.link:hover::after {
  @apply scale-x-100;
}
#description {
  @apply transition-all duration-500 opacity-0;
}
#description.active {
  @apply opacity-100;
}
</style>
