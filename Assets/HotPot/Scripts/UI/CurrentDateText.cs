using System;
using System.Globalization;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class CurrentDateText : MonoBehaviour
{
    public enum DisplayMode
    {
        FullDate = 0,
        DaysInMonth = 1
    }

    private static readonly string[] DayNames =
    {
        "Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư",
        "Thứ Năm", "Thứ Sáu", "Thứ Bảy"
    };

    [SerializeField, Tooltip("Kiểu nội dung cần hiển thị.")]
    private DisplayMode displayMode = DisplayMode.FullDate;

    [SerializeField, Tooltip("Số ngày lệch so với ngày hiện tại của máy.")]
    private int dayOffset = 0;

    [SerializeField, Tooltip("Tiền tố hiển thị trước thứ và ngày.")]
    private string prefix = string.Empty;

    private TMP_Text dateLabel;

    // EN: Cache the required text component before the first date refresh.
    // VI: Cache component chữ bắt buộc trước lần cập nhật ngày đầu tiên.
    private void Awake()
    {
        dateLabel = GetComponent<TMP_Text>();
    }

    // EN: Show the current month length or the offset date with its prefix whenever enabled.
    // VI: Hiển thị số ngày tháng hiện tại hoặc ngày lệch kèm tiền tố mỗi khi bật nhãn.
    private void OnEnable()
    {
        DateTime now = DateTime.Now;
        if (displayMode == DisplayMode.DaysInMonth)
        {
            dateLabel.text = $"Tháng {now.Month} có {DateTime.DaysInMonth(now.Year, now.Month)} ngày";
            return;
        }

        now = now.AddDays(dayOffset);
        dateLabel.text = prefix + DayNames[(int)now.DayOfWeek] + ", " +
            now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }
}
