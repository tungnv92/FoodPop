import type { Metadata } from 'next';
export const metadata: Metadata={title:'FoodPop Level Studio',description:'Thiết kế level và lưu phiên bản dùng chung.'};
// EN: Provides the minimal Vietnamese application shell.
// VI: Cung cấp khung ứng dụng tiếng Việt tối giản.
export default function RootLayout({children}:Readonly<{children:React.ReactNode}>){return <html lang="vi"><body>{children}</body></html>;}
