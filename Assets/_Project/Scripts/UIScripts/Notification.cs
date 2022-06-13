using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Notification : MonoBehaviour
{
	[SerializeField] private Image _notificationSlide = default;
	[SerializeField] private TextMeshProUGUI _notificationText = default;
	[Header("Notification Colors")]
	[SerializeField] private Color _punishColor = Color.red;
	[SerializeField] private Color _knockdownColor = Color.blue;
	[SerializeField] private Color _crossUpColor = Color.yellow;
	[SerializeField] private Color _guardBreakColor = Color.yellow;


	public void SetNotification(NotificationTypeEnum notificationType)
	{
		_notificationText.text = Regex.Replace(notificationType.ToString(), "([a-z])([A-Z])", "$1 $2");
		switch (notificationType)
		{
			case NotificationTypeEnum.Punish:
				_notificationSlide.color = _punishColor;
				_notificationText.color = _punishColor;
				break;
			case NotificationTypeEnum.Knockdown:
				_notificationSlide.color = _knockdownColor;
				_notificationText.color = _knockdownColor;
				break;
			case NotificationTypeEnum.CrossUp:
				_notificationSlide.color = _crossUpColor;
				_notificationText.color = _crossUpColor;
				break;
			case NotificationTypeEnum.GuardBreak:
				_notificationSlide.color = _guardBreakColor;
				_notificationText.color = _guardBreakColor;
				break;
		}
	}
}