# Blacksmith.sh Implementation Plan

> I'm using the writing-plans skill to create the implementation plan.

Goal: Rebuild https://www.blacksmith.sh in Next.js + TypeScript + Tailwind to achieve a functionally and visually identical site that is maintainable and testable.

Architecture: Next.js (app router recommended) with SSG for static pages and SSR for any dynamic endpoints. TailwindCSS for styling, Framer Motion for animations, API routes for forms.

Tech Stack: Next.js, React, TypeScript, TailwindCSS, Jest, React Testing Library, Playwright, Vercel (deploy target)

---

### Task 1: Project scaffold

Files:
- Create: `package.json`, `next.config.js`, `tailwind.config.js`, `postcss.config.js`
- Create: `app/layout.tsx`, `app/page.tsx`, `components/Header.tsx`, `components/Footer.tsx`

Step 1: Initialize package.json (we assume repo already has Next; if not, run)

Run: `npm init -y` (or use existing)
Expected: package.json created

Step 2: Install dependencies

Run: `npm install next react react-dom typescript @types/react @types/node tailwindcss postcss autoprefixer framer-motion jest @testing-library/react @testing-library/jest-dom playwright -D`
Expected: Dependencies installed

Step 3: Add Next/Tailwind basic config files (create exact files under root)

Step 4: Commit

Run: `git add package.json next.config.js tailwind.config.js postcss.config.js app components` && git commit -m "chore: scaffold Next.js + Tailwind project for blacksmith clone"`

### Task 2: Layout and global styles

Files:
- Create: `app/layout.tsx`
- Create: `styles/globals.css`

Step 1: Write failing test for layout rendering

Test: `tests/components/layout.test.tsx`
Run: `npx jest tests/components/layout.test.tsx -i`
Expected: Test fails (no layout)

Step 2: Implement layout and styles

Step 3: Run test and ensure it passes; commit changes

### Task 3: Hero component

Files:
- Create: `components/Hero.tsx`
- Test: `tests/components/hero.test.tsx`

Steps:
1. Write failing test asserting hero headline exists
2. Implement Hero.tsx with responsive design
3. Run test, ensure pass
4. Commit

### Task 4: Feature grid & cards

Files:
- Create: `components/FeatureGrid.tsx`, `components/FeatureCard.tsx`
- Test: `tests/components/feature.test.tsx`

Steps: TDD steps similar to above

### Task 5: Forms & API

Files:
- Create: `components/ContactForm.tsx`
- Create: `pages/api/contact.ts`
- Test: `tests/api/contact.test.ts`

Steps:
1. Write failing API test: POST /api/contact returns 200 with {ok:true}
2. Implement minimal API route storing/mocking submission
3. Implement client form, wire to API
4. Tests & commit

### Task 6: Assets & fonts

Files:
- Add assets to `/public/assets/*`

Steps:
1. Download images/fonts (ensure license). Optimize images
2. Add to /public/assets
3. Commit

### Task 7: Tests and E2E

Files:
- Create Playwright tests: `e2e/home.spec.ts`

Steps:
1. Write Playwright script that loads homepage, asserts hero text, clicks CTA, fills form (with API mocked)
2. Run Playwright, ensure pass
3. Commit tests

### Task 8: CI

Files:
- Create: `.github/workflows/ci.yml`

Steps:
1. Add job: install, build, run unit tests, run Playwright smoke tests
2. Commit

### Task 9: Final audit & polish

Steps:
1. Run lint/typecheck/build
2. Visual compare with original site and adjust styles
3. Prepare README with run/deploy instructions
4. Commit

---

Plan saved to `docs/plans/2026-02-20-blacksmith-clone-implementation-plan.md`.

Execution options:
1) Subagent-Driven: run tasks interactively in this session (recommended)
2) Parallel Session: hand off to a separate executing-plans session

Which execution option should I use? If none specified, I will proceed with Subagent-Driven execution and begin Task 1.
