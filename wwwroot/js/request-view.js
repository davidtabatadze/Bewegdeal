/**
 * Request View — Swiper Thumbs Gallery
 * v1.0.0
 *
 * `requestFiles` is always defined by the inline <script> in View.cshtml.
 */

'use strict';

(function () {

  const galleryThumbsEl = document.querySelector('.gallery-thumbs');
  const galleryTopEl    = document.querySelector('.gallery-top');

  let galleryInstance;

  if (galleryThumbsEl) {
    galleryInstance = new Swiper(galleryThumbsEl, {
      spaceBetween:          10,
      slidesPerView:         mediaCount,
      freeMode:              false,
      watchSlidesVisibility: true,
      watchSlidesProgress:   true
    });
  }

  if (galleryTopEl) {
    new Swiper(galleryTopEl, {
      spaceBetween: 10,
      navigation: {
        nextEl: '.swiper-button-next',
        prevEl: '.swiper-button-prev'
      },
      thumbs: {
        swiper: galleryInstance ?? null
      }
    });
  }

})();
