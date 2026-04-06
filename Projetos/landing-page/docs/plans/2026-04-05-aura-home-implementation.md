# Aura Home Website — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a single-page Aura Home marketing website embodying "technology you don't feel, only comfort."

**Architecture:** Vanilla HTML/CSS/JS with Canvas-based particle system. No framework dependencies. Five sections: Hero (particle canvas), Invisible Control (cursor-reactive), Scene Narrative (scroll-triggered crossfades), Folded Specs (manual accordion), Footer. All animations via CSS transitions and requestAnimationFrame.

**Tech Stack:** HTML5, CSS3 (custom properties, grid, flexbox), Canvas API, Google Fonts (Inter Tight, Source Serif 4)

---

## File Structure

```
index.html       — Single-page markup, all sections
styles.css        — Design system, components, animations
script.js         — Particle physics, scroll detection, interactions
docs/plans/2026-04-05-aura-home-design.md  — Design reference
```

---

## Task 1: HTML Structure & CSS Foundation

**Files:**
- Create: `index.html`
- Create: `styles.css`

**Step 1: Create index.html with all sections**

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Aura Home — Technology you don't feel</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300&family=Source+Serif+4:wght@400&display=swap" rel="stylesheet">
  <link rel="stylesheet" href="styles.css">
</head>
<body>

  <!-- Hero Section -->
  <section id="hero" class="hero">
    <canvas id="particle-canvas"></canvas>
    <div class="hero-content">
      <h1 class="hero-title">AURA HOME</h1>
    </div>
    <div class="scroll-indicator">
      <span class="scroll-line"></span>
    </div>
  </section>

  <!-- Invisible Control Section -->
  <section id="control" class="control-section">
    <div class="control-panel">
      <div class="panel-display">
        <div class="temp-display">
          <span class="temp-value">22.0</span>
          <span class="temp-unit">°C</span>
        </div>
        <div class="light-display">
          <span class="light-label">Reading</span>
        </div>
      </div>
    </div>
    <p class="control-hint">Move to experience</p>
  </section>

  <!-- Scene Narrative Section -->
  <section id="scenes" class="scenes-section">
    <div class="scene" data-scene="morning">
      <div class="scene-visual">
        <div class="scene-particles"></div>
      </div>
      <div class="scene-text">
        <h2>Morning</h2>
        <p>Light that knows when to wake you</p>
      </div>
    </div>
    <div class="light-strip"></div>
    <div class="scene" data-scene="reading">
      <div class="scene-visual"></div>
      <div class="scene-text">
        <h2>Reading</h2>
        <p>Pages turn, shadows adjust</p>
      </div>
    </div>
    <div class="light-strip"></div>
    <div class="scene" data-scene="bathing">
      <div class="scene-visual"></div>
      <div class="scene-text">
        <h2>Bathing</h2>
        <p>Steam rises, warmth holds</p>
      </div>
    </div>
    <div class="light-strip"></div>
    <div class="scene" data-scene="evening">
      <div class="scene-visual"></div>
      <div class="scene-text">
        <h2>Evening</h2>
        <p>The day settles, so do you</p>
      </div>
    </div>
  </section>

  <!-- Folded Specs Section -->
  <section id="specs" class="specs-section">
    <div class="spec-card">
      <div class="spec-tagline">0.1-second ultra-fast response</div>
      <div class="spec-details">
        <p>Our neural processing chip detects movement, light, and temperature shifts before you notice them. The home responds to life, not to commands.</p>
      </div>
    </div>
    <div class="spec-card">
      <div class="spec-tagline">Invisible presence detection</div>
      <div class="spec-details">
        <p>Millimeter-wave sensors map your home without cameras. Your privacy remains untouched, your movements understood.</p>
      </div>
    </div>
    <div class="spec-card">
      <div class="spec-tagline">19,000 color temperatures</div>
      <div class="spec-details">
        <p>From candlelight warmth to daylight clarity — each bulb renders light as nature intended, shifting so gradually your circadian rhythm stays intact.</p>
      </div>
    </div>
  </section>

  <!-- Footer -->
  <footer class="footer">
    <div class="footer-logo">AURA HOME</div>
    <p class="footer-tagline">Technology you don't feel</p>
    <nav class="footer-nav">
      <a href="#control">Control</a>
      <a href="#scenes">Scenes</a>
      <a href="#specs">Specs</a>
    </nav>
  </footer>

  <script src="script.js"></script>
</body>
</html>
```

**Step 2: Create styles.css with design system**

```css
:root {
  --bg: #F5F3F0;
  --primary: #E8E4DF;
  --secondary: #D4CFC7;
  --accent-halo: #F5E6D3;
  --text-primary: #3D3D3D;
  --text-secondary: #8A8580;
  --font-heading: 'Inter', sans-serif;
  --font-body: 'Source Serif 4', serif;
}

* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

html {
  scroll-behavior: smooth;
}

body {
  background: var(--bg);
  color: var(--text-primary);
  font-family: var(--font-body);
  line-height: 1.8;
  overflow-x: hidden;
}

/* Hero Section */
.hero {
  height: 100vh;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

#particle-canvas {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
}

.hero-content {
  position: relative;
  z-index: 10;
  text-align: center;
}

.hero-title {
  font-family: var(--font-heading);
  font-weight: 300;
  font-size: clamp(2rem, 8vw, 5rem);
  letter-spacing: 0.3em;
  color: var(--text-primary);
  opacity: 0;
  animation: fadeIn 2s ease-out 0.5s forwards;
}

@keyframes fadeIn {
  to { opacity: 1; }
}

.scroll-indicator {
  position: absolute;
  bottom: 2rem;
  left: 50%;
  transform: translateX(-50%);
}

.scroll-line {
  display: block;
  width: 1px;
  height: 60px;
  background: linear-gradient(to bottom, transparent, var(--text-secondary));
  animation: scrollPulse 2s ease-in-out infinite;
}

@keyframes scrollPulse {
  0%, 100% { opacity: 0.3; }
  50% { opacity: 1; }
}

/* Invisible Control Section */
.control-section {
  min-height: 80vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  background: var(--primary);
}

.control-panel {
  width: min(400px, 90vw);
  aspect-ratio: 16/9;
  background: linear-gradient(145deg, #F0EDE9, #E5E1DC);
  border-radius: 24px;
  box-shadow: 
    0 20px 60px rgba(0,0,0,0.05),
    inset 0 1px 0 rgba(255,255,255,0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: box-shadow 1s ease;
}

.panel-display {
  text-align: center;
}

.temp-display {
  font-family: var(--font-heading);
  font-size: clamp(3rem, 10vw, 5rem);
  font-weight: 300;
  letter-spacing: 0.05em;
  transition: color 1s ease;
}

.temp-unit {
  font-size: 0.4em;
  vertical-align: super;
}

.light-display {
  margin-top: 1rem;
  font-family: var(--font-body);
  font-size: 1.2rem;
  color: var(--text-secondary);
  transition: opacity 1s ease;
}

.control-hint {
  margin-top: 2rem;
  font-size: 0.9rem;
  color: var(--text-secondary);
  letter-spacing: 0.1em;
  opacity: 0;
  animation: fadeIn 1s ease-out 1s forwards;
}

/* Scene Narrative Section */
.scenes-section {
  padding: 8rem 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4rem;
}

.scene {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 3rem;
  width: 100%;
  max-width: 1000px;
  padding: 0 2rem;
  opacity: 0;
  transform: translateY(40px);
  transition: opacity 0.8s ease, transform 0.8s ease;
}

.scene.visible {
  opacity: 1;
  transform: translateY(0);
}

.scene:nth-child(odd) {
  flex-direction: column-reverse;
}

.scene-visual {
  width: min(500px, 80vw);
  aspect-ratio: 4/3;
  background: var(--primary);
  border-radius: 16px;
  position: relative;
  overflow: hidden;
}

.scene-particles {
  position: absolute;
  inset: 0;
  background: radial-gradient(ellipse at center, var(--accent-halo) 0%, transparent 70%);
}

.scene-text {
  text-align: center;
}

.scene-text h2 {
  font-family: var(--font-heading);
  font-weight: 300;
  font-size: clamp(1.5rem, 4vw, 2.5rem);
  letter-spacing: 0.2em;
  margin-bottom: 1rem;
}

.scene-text p {
  font-size: clamp(1rem, 2vw, 1.4rem);
  color: var(--text-secondary);
  max-width: 400px;
  margin: 0 auto;
}

.light-strip {
  width: 2px;
  height: 60px;
  background: linear-gradient(to bottom, var(--accent-halo), var(--secondary));
  border-radius: 1px;
  opacity: 0;
  transition: opacity 0.6s ease;
}

.light-strip.visible {
  opacity: 1;
  animation: stripPulse 3s ease-in-out infinite;
}

@keyframes stripPulse {
  0%, 100% { opacity: 0.5; }
  50% { opacity: 1; }
}

/* Specs Section */
.specs-section {
  padding: 8rem 2rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2rem;
  background: linear-gradient(to bottom, var(--bg), var(--primary));
}

.spec-card {
  width: min(600px, 90vw);
  background: #FAF8F5;
  border-radius: 12px;
  padding: 1.5rem 2rem;
  cursor: pointer;
  transition: transform 0.3s ease, box-shadow 0.3s ease;
  box-shadow: 0 4px 20px rgba(0,0,0,0.03);
}

.spec-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 30px rgba(0,0,0,0.05);
}

.spec-tagline {
  font-family: var(--font-heading);
  font-weight: 300;
  font-size: 1.1rem;
  letter-spacing: 0.05em;
  color: var(--text-primary);
}

.spec-details {
  max-height: 0;
  overflow: hidden;
  transition: max-height 0.6s ease-out, margin-top 0.6s ease-out;
}

.spec-card.expanded .spec-details {
  max-height: 200px;
  margin-top: 1rem;
}

.spec-details p {
  font-size: 0.95rem;
  color: var(--text-secondary);
  line-height: 1.7;
}

/* Footer */
.footer {
  padding: 6rem 2rem;
  text-align: center;
  background: linear-gradient(to bottom, var(--primary), var(--secondary));
}

.footer-logo {
  font-family: var(--font-heading);
  font-weight: 300;
  font-size: 1.5rem;
  letter-spacing: 0.3em;
  margin-bottom: 1rem;
}

.footer-tagline {
  font-size: 0.9rem;
  color: var(--text-secondary);
  margin-bottom: 2rem;
}

.footer-nav {
  display: flex;
  justify-content: center;
  gap: 2rem;
}

.footer-nav a {
  color: var(--text-secondary);
  text-decoration: none;
  font-size: 0.85rem;
  letter-spacing: 0.1em;
  transition: color 0.3s ease;
}

.footer-nav a:hover {
  color: var(--text-primary);
}
```

**Step 3: Verify HTML structure renders correctly**

Open in browser, verify all sections render without content overflow.

---

## Task 2: Particle System & Scroll Behavior

**Files:**
- Create: `script.js`

**Step 1: Create particle physics engine**

```javascript
class Particle {
  constructor(canvas, mode = 'away') {
    this.canvas = canvas;
    this.reset(mode);
  }

  reset(mode = 'away') {
    this.x = Math.random() * this.canvas.width;
    this.y = this.canvas.height + Math.random() * 100;
    this.size = Math.random() * 2 + 0.5;
    this.speedY = -(Math.random() * 0.3 + 0.1);
    this.speedX = (Math.random() - 0.5) * 0.2;
    this.opacity = mode === 'away' ? Math.random() * 0.2 : Math.random() * 0.6;
    this.wobble = Math.random() * Math.PI * 2;
    this.wobbleSpeed = Math.random() * 0.02 + 0.01;
  }

  update(mouseX, mouseY, mode) {
    this.wobble += this.wobbleSpeed;
    this.x += this.speedX + Math.sin(this.wobble) * 0.1;
    this.y += this.speedY;

    if (mouseX !== null) {
      const dx = this.x - mouseX;
      const dy = this.y - mouseY;
      const dist = Math.sqrt(dx * dx + dy * dy);
      if (dist < 150) {
        this.x += dx * 0.01;
        this.y += dy * 0.01;
      }
    }

    const targetOpacity = mode === 'away' ? 0.15 : 0.5;
    this.opacity += (targetOpacity - this.opacity) * 0.02;

    if (this.y < -10) {
      this.reset(mode);
    }
  }

  draw(ctx) {
    ctx.beginPath();
    ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
    ctx.fillStyle = `rgba(245, 230, 211, ${this.opacity})`;
    ctx.fill();
  }
}

class ParticleCanvas {
  constructor(canvasId) {
    this.canvas = document.getElementById(canvasId);
    this.ctx = this.canvas.getContext('2d');
    this.particles = [];
    this.mouseX = null;
    this.mouseY = null;
    this.mode = 'away';
    this.heroSection = document.getElementById('hero');
    
    this.resize();
    this.init();
    this.bindEvents();
    this.animate();
  }

  resize() {
    this.canvas.width = window.innerWidth;
    this.canvas.height = window.innerHeight;
  }

  init() {
    const count = window.innerWidth < 768 ? 60 : 100;
    for (let i = 0; i < count; i++) {
      const p = new Particle(this.canvas, this.mode);
      p.y = Math.random() * this.canvas.height;
      this.particles.push(p);
    }
  }

  bindEvents() {
    window.addEventListener('resize', () => this.resize());
    
    document.addEventListener('mousemove', (e) => {
      this.mouseX = e.clientX;
      this.mouseY = e.clientY;
    });

    window.addEventListener('scroll', () => {
      const scrolled = window.scrollY;
      const threshold = this.heroSection.offsetHeight * 0.3;
      this.mode = scrolled > threshold ? 'home' : 'away';
    });
  }

  drawLightBeam() {
    const gradient = this.ctx.createRadialGradient(
      this.canvas.width * 0.3, 0,
      0,
      this.canvas.width * 0.3, 0,
      this.canvas.height * 0.8
    );
    
    if (this.mode === 'away') {
      gradient.addColorStop(0, 'rgba(245, 243, 240, 0.03)');
      gradient.addColorStop(1, 'rgba(245, 243, 240, 0)');
    } else {
      gradient.addColorStop(0, 'rgba(245, 230, 211, 0.15)');
      gradient.addColorStop(0.5, 'rgba(245, 230, 211, 0.05)');
      gradient.addColorStop(1, 'rgba(245, 230, 211, 0)');
    }
    
    this.ctx.fillStyle = gradient;
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
  }

  animate() {
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    
    this.drawLightBeam();
    
    this.particles.forEach(p => {
      p.update(this.mouseX, this.mouseY, this.mode);
      p.draw(this.ctx);
    });
    
    requestAnimationFrame(() => this.animate());
  }
}

document.addEventListener('DOMContentLoaded', () => {
  if (document.getElementById('particle-canvas')) {
    new ParticleCanvas('particle-canvas');
  }
});
```

**Step 2: Test particle animation**

Open browser, verify particles drift upward with subtle wobble. Scroll to trigger home mode (warmer, denser particles).

---

## Task 3: Invisible Control Interaction

**Files:**
- Modify: `script.js`

**Step 1: Add cursor-reactive temperature and light display**

Add to script.js after ParticleCanvas class:

```javascript
class InvisibleControl {
  constructor() {
    this.panel = document.querySelector('.control-panel');
    this.tempDisplay = document.querySelector('.temp-value');
    this.lightLabel = document.querySelector('.light-label');
    this.baseTemp = 22.0;
    this.currentTemp = 22.0;
    this.targetTemp = 22.0;
    
    this.bindEvents();
    this.animate();
  }

  bindEvents() {
    document.addEventListener('mousemove', (e) => this.handleMouseMove(e));
  }

  handleMouseMove(e) {
    const rect = this.panel.getBoundingClientRect();
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;
    
    const dx = e.clientX - centerX;
    const dy = e.clientY - centerY;
    const distance = Math.sqrt(dx * dx + dy * dy);
    const maxDistance = 400;
    
    const proximity = Math.max(0, 1 - distance / maxDistance);
    
    this.targetTemp = this.baseTemp + (proximity * 1.5);
    
    if (this.lightLabel) {
      this.lightLabel.textContent = proximity > 0.3 ? 'Reading' : 'Relaxing';
    }
    
    if (this.panel) {
      const warmth = proximity * 0.1;
      this.panel.style.boxShadow = `
        0 20px 60px rgba(0,0,0,${0.05 - warmth * 0.03}),
        inset 0 1px 0 rgba(255,255,255,${0.8 + warmth * 0.1})
      `;
    }
  }

  animate() {
    this.currentTemp += (this.targetTemp - this.currentTemp) * 0.05;
    
    if (this.tempDisplay) {
      this.tempDisplay.textContent = this.currentTemp.toFixed(1);
    }
    
    requestAnimationFrame(() => this.animate());
  }
}
```

**Step 2: Initialize both systems**

Update DOMContentLoaded:

```javascript
document.addEventListener('DOMContentLoaded', () => {
  if (document.getElementById('particle-canvas')) {
    new ParticleCanvas('particle-canvas');
  }
  if (document.querySelector('.control-section')) {
    new InvisibleControl();
  }
});
```

**Step 3: Test cursor interaction**

Open browser, move cursor near panel. Verify temperature fluctuates smoothly and light label changes.

---

## Task 4: Scene Narrative & Spec Accordion

**Files:**
- Modify: `script.js`

**Step 1: Add scene scroll animation**

```javascript
class SceneNarrative {
  constructor() {
    this.scenes = document.querySelectorAll('.scene');
    this.strips = document.querySelectorAll('.light-strip');
    this.intersectionObserver = new IntersectionObserver(
      (entries) => this.handleIntersection(entries),
      { threshold: 0.3 }
    );
    this.init();
  }

  init() {
    this.scenes.forEach(scene => this.intersectionObserver.observe(scene));
    this.strips.forEach(strip => this.intersectionObserver.observe(strip));
  }

  handleIntersection(entries) {
    entries.forEach(entry => {
      if (entry.target.classList.contains('scene')) {
        if (entry.isIntersecting) {
          entry.target.classList.add('visible');
        }
      } else if (entry.target.classList.contains('light-strip')) {
        if (entry.isIntersecting) {
          entry.target.classList.add('visible');
        }
      }
    });
  }
}
```

**Step 2: Add spec accordion interaction**

```javascript
class SpecAccordion {
  constructor() {
    this.cards = document.querySelectorAll('.spec-card');
    this.init();
  }

  init() {
    this.cards.forEach(card => {
      card.addEventListener('click', () => {
        const wasExpanded = card.classList.contains('expanded');
        
        this.cards.forEach(c => c.classList.remove('expanded'));
        
        if (!wasExpanded) {
          card.classList.add('expanded');
        }
      });
    });
  }
}
```

**Step 3: Initialize scene and spec systems**

Update DOMContentLoaded:

```javascript
if (document.querySelector('.scenes-section')) {
  new SceneNarrative();
}
if (document.querySelector('.specs-section')) {
  new SpecAccordion();
}
```

**Step 4: Test all interactions**

Open browser, scroll through scenes (verify fade-in), click spec cards (verify accordion expand/collapse).

---

## Task 5: Navigation & Scroll Indicator

**Files:**
- Modify: `styles.css`
- Modify: `script.js`

**Step 1: Add navigation fade-in on scroll**

Add to styles.css:

```css
.footer-nav a {
  opacity: 0;
  transform: translateY(10px);
  transition: opacity 0.5s ease, color 0.3s ease, transform 0.5s ease;
}

.footer-nav.visible a {
  opacity: 1;
  transform: translateY(0);
}

.scroll-indicator.hidden {
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.5s ease;
}
```

Add to script.js:

```javascript
class NavigationBehavior {
  constructor() {
    this.lastScrollY = 0;
    this.scrollIndicator = document.querySelector('.scroll-indicator');
    this.footerNav = document.querySelector('.footer-nav');
    this.bindEvents();
  }

  bindEvents() {
    window.addEventListener('scroll', () => this.handleScroll());
  }

  handleScroll() {
    const currentScrollY = window.scrollY;
    
    if (this.scrollIndicator && currentScrollY > 100) {
      this.scrollIndicator.classList.add('hidden');
    }
    
    if (this.footerNav && currentScrollY > window.innerHeight * 0.5) {
      this.footerNav.classList.add('visible');
    }
    
    this.lastScrollY = currentScrollY;
  }
}
```

**Step 2: Initialize navigation behavior**

Update DOMContentLoaded:

```javascript
if (document.querySelector('.footer-nav')) {
  new NavigationBehavior();
}
```

---

## Task 6: Polish & Responsive

**Files:**
- Modify: `styles.css`

**Step 1: Add responsive breakpoints**

```css
@media (max-width: 768px) {
  .hero-title {
    letter-spacing: 0.2em;
  }
  
  .scene {
    gap: 2rem;
  }
  
  .scene:nth-child(odd) {
    flex-direction: column-reverse;
  }
  
  .control-panel {
    aspect-ratio: 4/3;
  }
  
  .footer-nav {
    flex-direction: column;
    gap: 1rem;
  }
}
```

**Step 2: Add reduced motion support**

```css
@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

---

## Task 7: Final Verification

**Step 1: Run browser verification**

Open index.html in browser, verify:
- [ ] Particle canvas renders and animates
- [ ] Scroll changes particle mode (away → home)
- [ ] Temperature display responds to cursor proximity
- [ ] Scenes fade in on scroll
- [ ] Spec cards expand/collapse on click
- [ ] Navigation fades in on scroll
- [ ] No console errors
- [ ] Responsive on mobile viewport

**Step 2: Commit**

```bash
git add index.html styles.css script.js
git commit -m "feat: implement Aura Home website with particle system and interactive sections"
```
