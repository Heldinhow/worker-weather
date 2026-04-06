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

document.addEventListener('DOMContentLoaded', () => {
  if (document.getElementById('particle-canvas')) {
    new ParticleCanvas('particle-canvas');
  }
  if (document.querySelector('.control-section')) {
    new InvisibleControl();
  }
  if (document.querySelector('.scenes-section')) {
    new SceneNarrative();
  }
  if (document.querySelector('.specs-section')) {
    new SpecAccordion();
  }
  if (document.querySelector('.footer-nav')) {
    new NavigationBehavior();
  }
});
