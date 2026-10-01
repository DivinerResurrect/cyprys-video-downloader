/* Shared script for every language version.
   Texts, links and SEO tags are baked into the HTML by build.mjs;
   this file only adds behaviour. */
(function () {
  const root = document.documentElement;
  const repo = root.dataset.repo || "";

  /* Live star count once the repo has a meaningful number of stars */
  const line = document.querySelector("[data-stars-line]");
  if (line && repo && !/^USERNAME\//.test(repo)) {
    fetch("https://api.github.com/repos/" + repo)
      .then(r => (r.ok ? r.json() : null))
      .then(d => {
        if (!d || d.stargazers_count < 50) return;
        const n = d.stargazers_count.toLocaleString(root.lang);
        const strong = line.querySelector("strong");
        if (strong && strong.hasAttribute("data-stars")) strong.textContent = n;
      })
      .catch(() => {});
  }

  /* Language menu: close on outside click and Escape */
  const lang = document.querySelector(".lang");
  if (lang) {
    document.addEventListener("click", e => { if (!lang.contains(e.target)) lang.open = false; });
    document.addEventListener("keydown", e => {
      if (e.key === "Escape" && lang.open) { lang.open = false; lang.querySelector("summary").focus(); }
    });
  }
})();
