import { redirect } from 'next/navigation';
// EN: Opens the preserved editor without changing its local storage origin.
// VI: Mở editor đã giữ nguyên, không thay đổi origin lưu trữ local.
export default function Home(){redirect('/studio/index.html');}
