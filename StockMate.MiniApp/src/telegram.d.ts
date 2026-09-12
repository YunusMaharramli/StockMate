interface TelegramDownloadFileParams {
  url: string;
  file_name: string;
}

interface TelegramWebApp {
  readonly initData: string;
  readonly colorScheme: 'light' | 'dark';
  readonly version?: string;
  readonly platform?: string;
  ready: () => void;
  expand: () => void;
  isVersionAtLeast?: (version: string) => boolean;
  openLink?: (url: string) => void;
  downloadFile?: (
    params: TelegramDownloadFileParams,
    callback?: (accepted: boolean) => void,
  ) => void;
}

declare global {
  interface Window {
    Telegram?: {
      WebApp: TelegramWebApp;
    };
  }
}

export {};
