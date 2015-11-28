namespace Assets.Data{
	public static class CardFace{
		/** 普列塞装甲 */
		public const int CF_PuLieSaiZhuangJia = 5;
		/** 深海威压 */
		public const int CF_ShenHaiWeiYa = 29;
		/** 以逸待劳 */
		public const int CF_YiYiDaiLao = 31;
		/** 进击 */
		public const int CF_JinJi = 9;
		/** 虎虎虎 */
		public const int CF_HuHuHu = 28;
		/** 存在舰队 */
		public const int CF_CunZaiJianDui = 23;
		/** 战场突入 */
		public const int CF_ZhanChangTuRu = 17;
		/** 金马达 */
		public const int CF_JinMaDa = 8;
		/** 休憩的夏至 */
		public const int CF_XiuQiDiXiaZhi = 7;
		/** 彩防空 */
		public const int CF_CaiFangKong = 22;
		/** 版本更新 */
		public const int CF_BanBenGengXin = 6;
		/** 莱茵河行动 */
		public const int CF_LaiYinHeHangDong = 13;
		/** U国后勤 */
		public const int CF_UGuoHouQin = 3;
		/** 火控雷达 */
		public const int CF_HuoKongLeiDa = 26;
		/** 深弹投射 */
		public const int CF_ShenDanTouShe = 25;
		/** 蓝马达 */
		public const int CF_LanMaDa = 11;
		/** 声呐 */
		public const int CF_ShengNa = 12;
		/** 一号作战 */
		public const int CF_YiHaoZuoZhan = 21;
		/** 回避 */
		public const int CF_HuiBi = 1;
		/** 辣条 */
		public const int CF_LaTiao = 18;
		/** 修理 */
		public const int CF_XiuLi = 2;
		/** 战术迂回 */
		public const int CF_ZhanShuYuHui = 16;
		/** 同受折磨 */
		public const int CF_TongShouZheMo = 19;
		/** 对海雷达 */
		public const int CF_DuiHaiLeiDa = 20;
		/** B25 */
		public const int CF_B25 = 10;
		/** 附加装甲 */
		public const int CF_FuJiaZhuangJia = 14;
		/** 警戒雷达 */
		public const int CF_JingJieLeiDa = 15;
		/** 发烟筒 */
		public const int CF_FaYanTong = 4;
		/** 金防空 */
		public const int CF_JinFangKong = 24;
		/** 突击！ */
		public const int CF_TuJi = 27;
		/** 破交战 */
		public const int CF_PoJiaoZhan = 30;

		public static string getName(int face) {
			switch(face) {
			case CF_PuLieSaiZhuangJia: return "普列塞装甲";
			case CF_ShenHaiWeiYa: return "深海威压";
			case CF_YiYiDaiLao: return "以逸待劳";
			case CF_JinJi: return "进击";
			case CF_HuHuHu: return "虎虎虎";
			case CF_CunZaiJianDui: return "存在舰队";
			case CF_ZhanChangTuRu: return "战场突入";
			case CF_JinMaDa: return "金马达";
			case CF_XiuQiDiXiaZhi: return "休憩的夏至";
			case CF_CaiFangKong: return "彩防空";
			case CF_BanBenGengXin: return "版本更新";
			case CF_LaiYinHeHangDong: return "莱茵河行动";
			case CF_UGuoHouQin: return "U国后勤";
			case CF_HuoKongLeiDa: return "火控雷达";
			case CF_ShenDanTouShe: return "深弹投射";
			case CF_LanMaDa: return "蓝马达";
			case CF_ShengNa: return "声呐";
			case CF_YiHaoZuoZhan: return "一号作战";
			case CF_HuiBi: return "回避";
			case CF_LaTiao: return "辣条";
			case CF_XiuLi: return "修理";
			case CF_ZhanShuYuHui: return "战术迂回";
			case CF_TongShouZheMo: return "同受折磨";
			case CF_DuiHaiLeiDa: return "对海雷达";
			case CF_B25: return "B25";
			case CF_FuJiaZhuangJia: return "附加装甲";
			case CF_JingJieLeiDa: return "警戒雷达";
			case CF_FaYanTong: return "发烟筒";
			case CF_JinFangKong: return "金防空";
			case CF_TuJi: return "突击！";
			case CF_PoJiaoZhan: return "破交战";
			}
			return null;
		}
	}
}