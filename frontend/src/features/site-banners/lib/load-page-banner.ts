import { cache } from 'react';
import { pageBannerDefaults } from '../content/page-banners';
import type { PageBannerData, PageBannerPage } from '../types/page-banner.types';

/**
 * What a public page's top banner says and shows. Every part of every banner comes through here,
 * so when the staff console can edit them (a proposed extension that awaits approval), this body
 * becomes an API read that falls back to the same defaults, and no page changes.
 */
export const loadPageBanner = cache(async (page: PageBannerPage): Promise<PageBannerData> => pageBannerDefaults[page]);
