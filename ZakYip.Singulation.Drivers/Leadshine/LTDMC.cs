using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;


// æŠ‘åˆ¶å‚å•†SDKç»“æ„ä½“çš„æœªä½¿ç”¨å­—æ®µè­¦å‘Š
#pragma warning disable CS0169

namespace csLTDMC //ÃüÃû¿Õ¼ä¸ù¾İÓ¦ÓÃ³ÌĞòĞŞ¸Ä
{

    public struct struct_hs_cmp_info
    {
    double start_pos;   //ÏßĞÔ±È½ÏÆğÊ¼µãÎ»ÖÃ.
    double interval;    //¼ä¾à.
    int count;//¸öÊı
    };


    public delegate uint DMC3K5K_OPERATE(IntPtr operate_data); 
    public partial class LTDMC
    {
        //ÉèÖÃºÍ¶ÁÈ¡´òÓ¡Ä£Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_debug_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_debug_mode(UInt16 mode, string FileName);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_debug_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_debug_mode(ref UInt16 mode, IntPtr FileName);
        //---------------------   °å¿¨³õÊ¼ºÍÅäÖÃº¯Êı  ----------------------
        //³õÊ¼»¯¿ØÖÆ¿¨£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_board_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_board_init();
        //Ó²¼ş¸´Î»£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_board_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_board_reset();
        //¹Ø±Õ¿ØÖÆ¿¨£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_board_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_board_close();
        //¿ØÖÆ¿¨ÈÈ¸´Î»£¨ÊÊÓÃÓÚEtherCAT¡¢RTEX×ÜÏß¿¨£©  
        [DllImport("LTDMC.dll")]
        public static extern short dmc_soft_reset(ushort CardNo);
        //¿ØÖÆ¿¨Àä¸´Î»£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cool_reset(ushort CardNo);
        //¿ØÖÆ¿¨³õÊ¼¸´Î»£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_original_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_original_reset(ushort CardNo);
        //¶ÁÈ¡¿ØÖÆ¿¨ĞÅÏ¢ÁĞ±í£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_CardInfList", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_CardInfList(ref UInt16 CardNum, UInt32[] CardTypeList, UInt16[] CardIdList);
        //¶ÁÈ¡·¢²¼°æ±¾ºÅ£¨ÊÊÓÃÓÚDMC3000/DMC5X10ÏµÁĞÂö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_card_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_card_version(UInt16 CardNo, ref UInt32 CardVersion);
        //¶ÁÈ¡¿ØÖÆ¿¨Ó²¼şµÄ¹Ì¼ş°æ±¾£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_card_soft_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_card_soft_version(UInt16 CardNo, ref UInt32 FirmID, ref UInt32 SubFirmID);
        //¶ÁÈ¡¿ØÖÆ¿¨¶¯Ì¬¿â°æ±¾£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_card_lib_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_card_lib_version(ref UInt32 LibVer);
        //¶ÁÈ¡·¢²¼°æ±¾ºÅ£¨ÊÊÓÃÓÚDMC3000/DMC5X10ÏµÁĞÂö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_release_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_release_version(ushort ConnectNo, byte[] ReleaseVersion);
        //¶ÁÈ¡Ö¸¶¨¿¨ÖáÊı£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_total_axes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_total_axes(UInt16 CardNo, ref UInt32 TotalAxis);
        //»ñÈ¡±¾µØIOµãÊı£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_total_ionum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_total_ionum(ushort CardNo, ref ushort TotalIn, ref ushort TotalOut);
        //»ñÈ¡±¾µØADDAÊäÈëÊä³öÊı£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_total_adcnum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_total_adcnum(ushort CardNo, ref ushort TotalIn, ref ushort TotalOut);
        //¶ÁÈ¡Ö¸¶¨¿¨²å²¹×ø±êÏµÊı£¨±£Áô£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_total_liners", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_total_liners(UInt16 CardNo, ref UInt32 TotalLiner);
        //¶¨ÖÆÀà£¨±£Áô£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_board_init_onecard", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_board_init_onecard(ushort CardNo);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_board_close_onecard", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_board_close_onecard(ushort CardNo);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_board_reset_onecard", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_board_reset_onecard(ushort CardNo);

        //ÃÜÂëº¯Êı£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_write_sn", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_sn(UInt16 CardNo, string new_sn);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_check_sn", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_check_sn(UInt16 CardNo, string check_sn);
        //µÇÈësn20191101£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_enter_password_ex(UInt16 CardNo, string str_pass);

        //---------------------ÔË¶¯Ä£¿éÂö³åÄ£Ê½------------------
        //Âö³åÄ£Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©	
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_pulse_outmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_pulse_outmode(UInt16 CardNo, UInt16 axis, UInt16 outmode);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_pulse_outmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_pulse_outmode(UInt16 CardNo, UInt16 axis, ref UInt16 outmode);
        //Âö³åµ±Á¿£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_equiv", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_equiv(UInt16 CardNo, UInt16 axis, ref double equiv);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_equiv", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_equiv(UInt16 CardNo, UInt16 axis, double equiv);
        //·´Ïò¼äÏ¶(Âö³å)£¨ÊÊÓÃÓÚDMC5000ÏµÁĞÂö³å¿¨£©	
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_backlash_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_backlash_unit(UInt16 CardNo, UInt16 axis, double backlash); 
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_backlash_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_backlash_unit(UInt16 CardNo, UInt16 axis, ref double backlash);

        //Í¨ÓÃÎÄ¼şÏÂÔØ
        [DllImport("LTDMC.dll", EntryPoint = "dmc_download_file", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_download_file(ushort CardNo, string pfilename, byte[] pfilenameinControl, ushort filetype);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_upload_file", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_upload_file(ushort CardNo, string pfilename, byte[] pfilenameinControl, ushort filetype);
        //ÏÂÔØÄÚ´æÎÄ¼ş ×ÜÏß¿¨£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_download_memfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_download_memfile(ushort CardNo, byte[] pbuffer, uint buffsize, byte[] pfilenameinControl, ushort filetype);
        //ÉÏ´«ÄÚ´æÎÄ¼ş£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_upload_memfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_upload_memfile(ushort CardNo, byte[] pbuffer, uint buffsize, byte[] pfilenameinControl, ref uint puifilesize, ushort filetype);
        //ÎÄ¼ş½ø¶È£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_progress(ushort CardNo, ref float process);
        //ÏÂÔØ²ÎÊıÎÄ¼ş£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_download_configfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_download_configfile(UInt16 CardNo, String FileName);
        //ÏÂÔØ¹Ì¼şÎÄ¼ş£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_download_firmware", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_download_firmware(UInt16 CardNo, String FileName);

        //----------------------ÏŞÎ»Òì³£ÉèÖÃ-------------------------------	
        //ÉèÖÃ¶ÁÈ¡ÈíÏŞÎ»²ÎÊı£¨ÊÊÓÃÓÚE3032×ÜÏß¿¨¡¢R3032×ÜÏß¿¨¡¢DMC3000/5000/5X10ÏµÁĞÂö³å¿¨£©	
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_softlimit(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 source_sel, UInt16 SL_action, Int32 N_limit, Int32 P_limit);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_softlimit(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 source_sel, ref UInt16 SL_action, ref Int32 N_limit, ref Int32 P_limit);
        //ÉèÖÃ¶ÁÈ¡ÈíÏŞÎ»²ÎÊıunit£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_softlimit_unit(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 source_sel, UInt16 SL_action, double N_limit, double P_limit);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_softlimit_unit(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 source_sel, ref UInt16 SL_action, ref double N_limit, ref double P_limit);
        //ÉèÖÃ¶ÁÈ¡ELĞÅºÅ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_el_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_el_mode(UInt16 CardNo, UInt16 axis, UInt16 el_enable, UInt16 el_logic, UInt16 el_mode);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_el_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_el_mode(UInt16 CardNo, UInt16 axis, ref UInt16 el_enable, ref UInt16 el_logic, ref UInt16 el_mode);
        //ÉèÖÃ¶ÁÈ¡EMGĞÅºÅ£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_emg_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_emg_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 emg_logic);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_emg_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_emg_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enbale, ref UInt16 emg_logic);
        //Íâ²¿¼õËÙÍ£Ö¹ĞÅºÅ¼°¼õËÙÍ£Ö¹Ê±¼äÉèÖÃ£¬ºÁÃëÎªµ¥Î»£¨±£Áô£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_dstp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_dstp_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 logic, UInt32 time);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_dstp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_dstp_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 logic, ref UInt32 time);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_dstp_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_dstp_time(UInt16 CardNo, UInt16 axis, UInt32 time);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_dstp_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_dstp_time(UInt16 CardNo, UInt16 axis, ref UInt32 time);
        //Íâ²¿¼õËÙÍ£Ö¹ĞÅºÅ¼°¼õËÙÍ£Ö¹Ê±¼äÉèÖÃ£¬ÃëÎªµ¥Î»£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_io_dstp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_io_dstp_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 logic);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_io_dstp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_io_dstp_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 logic);
        //µãÎ»ÔË¶¯¼õËÙÍ£Ö¹Ê±¼äÉèÖÃ¶ÁÈ¡£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_dec_stop_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_dec_stop_time(UInt16 CardNo, UInt16 axis, double stop_time);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_dec_stop_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_dec_stop_time(UInt16 CardNo, UInt16 axis, ref double stop_time);
        //²å²¹¼õËÙÍ£Ö¹ĞÅºÅºÍ¼õËÙÊ±¼äÉèÖÃ£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢EthreCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_vector_dec_stop_time(UInt16 CardNo, UInt16 Crd, double stop_time);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_vector_dec_stop_time(UInt16 CardNo, UInt16 Crd, ref double stop_time);
        //IO¼õËÙÍ£Ö¹¾àÀë£¨ÊÊÓÃÓÚDMC3000¡¢DMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_dec_stop_dist(UInt16 CardNo, UInt16 axis, Int32 dist);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_dec_stop_dist(UInt16 CardNo, UInt16 axis, ref Int32 dist);
        //IO¼õËÙÍ£Ö¹£¬Ö§³Öpmove/vmoveÔË¶¯£¨ÊÊÓÃÓÚDMC3000¡¢DMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_io_exactstop(UInt16 CardNo, UInt16 axis, UInt16 ioNum, UInt16[] ioList, UInt16 enable, UInt16 valid_logic, UInt16 action, UInt16 move_dir);       
        //ÉèÖÃÍ¨ÓÃÊäÈë¿ÚµÄÒ»Î»¼õËÙÍ£Ö¹IO¿Ú£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_io_dstp_bitno(UInt16 CardNo, UInt16 axis, UInt16 bitno, double filter);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_io_dstp_bitno(UInt16 CardNo, UInt16 axis, ref UInt16 bitno, ref double filter);

        //---------------------------µ¥ÖáÔË¶¯----------------------
        //Éè¶¨¶ÁÈ¡ËÙ¶ÈÇúÏß²ÎÊı	£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_profile(UInt16 CardNo, UInt16 axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double stop_vel);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_profile(UInt16 CardNo, UInt16 axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double stop_vel);
        //ËÙ¶ÈÉèÖÃ(Âö³åµ±Á¿)£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©	
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_profile_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_profile_unit(UInt16 CardNo, UInt16 Axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel);   //µ¥ÖáËÙ¶È²ÎÊı
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_profile_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_profile_unit(UInt16 CardNo, UInt16 Axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double Stop_Vel);
        //ËÙ¶ÈÇúÏßÉèÖÃ£¬¼ÓËÙ¶ÈÖµ±íÊ¾(Âö³å)£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_acc_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_acc_profile(UInt16 CardNo, UInt16 axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double stop_vel);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_acc_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_acc_profile(UInt16 CardNo, UInt16 axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double stop_vel);
        //ËÙ¶ÈÇúÏßÉèÖÃ£¬¼ÓËÙ¶ÈÖµ±íÊ¾(µ±Á¿)£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_profile_unit_acc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_profile_unit_acc(UInt16 CardNo, UInt16 Axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel);   //µ¥ÖáËÙ¶È²ÎÊı
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_profile_unit_acc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_profile_unit_acc(UInt16 CardNo, UInt16 Axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double Stop_Vel);      
        //ÉèÖÃ¶ÁÈ¡Æ½»¬ËÙ¶ÈÇúÏß²ÎÊı£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_s_profile(UInt16 CardNo, UInt16 axis, UInt16 s_mode, double s_para);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_s_profile(UInt16 CardNo, UInt16 axis, UInt16 s_mode, ref double s_para);            
        //µãÎ»ÔË¶¯(Âö³å)£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_pmove(UInt16 CardNo, UInt16 axis, Int32 Dist, UInt16 posi_mode);
        //µãÎ»ÔË¶¯(µ±Á¿)£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_pmove_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_pmove_unit(UInt16 CardNo, UInt16 axis, double Dist, UInt16 posi_mode);  
        //Ö¸¶¨Öá×ö¶¨³¤Î»ÒÆÔË¶¯ Í¬Ê±·¢ËÍËÙ¶ÈºÍSÊ±¼ä(Âö³å)£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©	
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pmove_extern(UInt16 CardNo, UInt16 axis, double dist, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double stop_Vel, double s_para, UInt16 posi_mode);
        //ÔÚÏß±äÎ»(Âö³å)£¬ÔË¶¯ÖĞ¸Ä±äÄ¿±êÎ»ÖÃ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_reset_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_reset_target_position(UInt16 CardNo, UInt16 axis, Int32 dist, UInt16 posi_mode);
        //±äËÙ±äÎ»(µ±Á¿)£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_reset_target_position_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_reset_target_position_unit(UInt16 CardNo, UInt16 Axis, double New_Pos); 
        //ÔÚÏß±äËÙ(Âö³å)£¬ÔË¶¯ÖĞ¸Ä±äÖ¸¶¨ÖáµÄµ±Ç°ÔË¶¯ËÙ¶È£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_change_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_change_speed(UInt16 CardNo, UInt16 axis, double Curr_Vel, double Taccdec);
        //ÔÚÏß±äËÙ(µ±Á¿)£¬ÔË¶¯ÖĞ¸Ä±äÖ¸¶¨ÖáµÄµ±Ç°ÔË¶¯ËÙ¶È£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_change_speed_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_change_speed_unit(UInt16 CardNo, UInt16 Axis, double New_Vel, double Taccdec);    
        //ÎŞÂÛÔË¶¯Óë·ñÇ¿ĞĞ¸Ä±äÄ¿±êÎ»ÖÃ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_update_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_update_target_position(UInt16 CardNo, UInt16 axis, Int32 dist, UInt16 posi_mode);
        //Ç¿ĞĞ±äÎ»À©Õ¹£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_update_target_position_extern(UInt16 CardNo, UInt16 axis, double mid_pos, double aim_pos, double vel, UInt16 posi_mode);
        //ÔÚÏß±äËÙ(µ±Á¿)£¬ÔË¶¯ÖĞ¸Ä±äÖ¸¶¨ÖáµÄµ±Ç°ÔË¶¯ËÙ¶È£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_update_target_position_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_update_target_position_unit(UInt16 CardNo, UInt16 Axis, double New_Pos);

      

        //---------------------JOGÔË¶¯--------------------
        //µ¥ÖáÁ¬ĞøËÙ¶ÈÔË¶¯£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©	
        [DllImport("LTDMC.dll", EntryPoint = "dmc_vmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_vmove(UInt16 CardNo, UInt16 axis, UInt16 dir);

        //---------------------²å²¹ÔË¶¯--------------------
        //²å²¹ËÙ¶ÈÉèÖÃ(Âö³å)£¨ÊÊÓÃÓÚDMC3000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_vector_profile_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_vector_profile_multicoor(UInt16 CardNo, UInt16 Crd, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_vector_profile_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_vector_profile_multicoor(UInt16 CardNo, UInt16 Crd, ref double Min_Vel, ref double Max_Vel, ref double Taccdec, ref double Tdec, ref double Stop_Vel);       
        //ÉèÖÃ¶ÁÈ¡Æ½»¬ËÙ¶ÈÇúÏß²ÎÊı£¨ÊÊÓÃÓÚDMC3000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_vector_s_profile_multicoor(UInt16 CardNo, UInt16 Crd, UInt16 s_mode, double s_para);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_vector_s_profile_multicoor(UInt16 CardNo, UInt16 Crd, UInt16 s_mode, ref double s_para);
        //²å²¹ËÙ¶È²ÎÊı(µ±Á¿)£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_vector_profile_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_vector_profile_unit(UInt16 CardNo, UInt16 Crd, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel);   //µ¥¶Î²å²¹ËÙ¶È²ÎÊı
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_vector_profile_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_vector_profile_unit(UInt16 CardNo, UInt16 Crd, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double Stop_Vel);
        //ÉèÖÃÆ½»¬ËÙ¶ÈÇúÏß²ÎÊı£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_vector_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_vector_s_profile(UInt16 CardNo, UInt16 Crd, UInt16 s_mode, double s_para);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_vector_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_vector_s_profile(UInt16 CardNo, UInt16 Crd, UInt16 s_mode, ref double s_para);
        //Ö±Ïß²å²¹ÔË¶¯£¨ÊÊÓÃÓÚDMC3000ÏµÁĞÂö³å¿¨£©	
        [DllImport("LTDMC.dll", EntryPoint = "dmc_line_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_line_multicoor(UInt16 CardNo, UInt16 crd, UInt16 axisNum, UInt16[] axisList, Int32[] DistList, UInt16 posi_mode);
        //Ô²»¡²å²¹ÔË¶¯£¨ÊÊÓÃÓÚDMC3000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_arc_move_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_arc_move_multicoor(UInt16 CardNo, UInt16 crd, UInt16[] AxisList, Int32[] Target_Pos, Int32[] Cen_Pos, UInt16 Arc_Dir, UInt16 posi_mode);
        //Ö±Ïß²å²¹(µ±Á¿)£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_line_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_line_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, UInt16 posi_mode);    //µ¥¶ÎÖ±Ïß
        //Ô²ĞÄÔ²»¡²å²¹(µ±Á¿)£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_arc_move_center_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_arc_move_center_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double[] Cen_Pos, UInt16 Arc_Dir, Int32 Circle, UInt16 posi_mode);     
//Ô²ĞÄÖÕµãÊ½Ô²»¡/ÂİĞıÏß/½¥¿ªÏß
        //°ë¾¶Ô²»¡²å²¹(µ±Á¿)£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_arc_move_radius_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_arc_move_radius_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double Arc_Radius, UInt16 Arc_Dir, Int32 Circle, UInt16 posi_mode);    
//°ë¾¶ÖÕµãÊ½Ô²»¡/ÂİĞıÏß
        //ÈıµãÔ²»¡²å²¹(µ±Á¿)£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_arc_move_3points_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_arc_move_3points_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double[] Mid_Pos, Int32 Circle, UInt16 posi_mode);     //ÈıµãÊ½Ô²»¡/ÂİĞıÏß
        //¾ØĞÎ²å²¹(µ±Á¿)£¨ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨¡¢RTEX×ÜÏß¿¨¡¢DMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_rectangle_move_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_rectangle_move_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] TargetPos, double[] MaskPos, Int32 Count, UInt16 rect_mode, UInt16 posi_mode);     
//¾ØĞÎÇøÓò²å²¹£¬µ¥¶Î²å²¹Ö¸Áî

        //----------------------PVTÔË¶¯---------------------------
        //PVTÔË¶¯¾É°æ £¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_PvtTable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_PvtTable(UInt16 CardNo, UInt16 iaxis, UInt32 count, double[] pTime, Int32[] pPos, double[] pVel);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_PtsTable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_PtsTable(UInt16 CardNo, UInt16 iaxis, UInt32 count, double[] pTime, Int32[] pPos, double[] pPercent);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_PvtsTable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_PvtsTable(UInt16 CardNo, UInt16 iaxis, UInt32 count, double[] pTime, Int32[] pPos, double velBegin, double velEnd);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_PttTable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_PttTable(UInt16 CardNo, UInt16 iaxis, UInt32 count, double[] pTime, int[] pPos);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_PvtMove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_PvtMove(UInt16 CardNo, UInt16 AxisNum, UInt16[] AxisList);
        //PVT»º³åÇøÌí¼Ó
        [DllImport("LTDMC.dll")]
        public static extern short dmc_PttTable_add(UInt16 CardNo, UInt16 iaxis, UInt16 count, double[] pTime, long[] pPos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_PtsTable_add(UInt16 CardNo, UInt16 iaxis, UInt16 count, double[] pTime, long[] pPos, double[] pPercent);
        //¶ÁÈ¡pvtÊ£Óà¿Õ¼ä
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pvt_get_remain_space(UInt16 CardNo, UInt16 iaxis);
        //PVTÔË¶¯ ×ÜÏß¿¨ĞÂ¹æ»®£¬ÊÊÓÃÓÚEtherCAT×ÜÏß¿¨
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pvt_table_unit(UInt16 CardNo, UInt16 iaxis, UInt32 count, double[] pTime, double[] pPos, double[] pVel);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pts_table_unit(UInt16 CardNo, UInt16 iaxis, UInt32 count, double[] pTime, double[] pPos, double[] pPercent);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pvts_table_unit(UInt16 CardNo, UInt16 iaxis, UInt32 count, double[] pTime, double[] pPos, double velBegin, double velEnd);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_ptt_table_unit(UInt16 CardNo, UInt16 iaxis, UInt32 count, double[] pTime, double[] pPos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pvt_move(UInt16 CardNo, UInt16 AxisNum, UInt16[] AxisList);
        //ÆäËüÀà£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_SetGearProfile(UInt16 CardNo, UInt16 axis, UInt16 MasterType, UInt16 MasterIndex, Int32 MasterEven, Int32 SlaveEven, UInt32 MasterSlope);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_GetGearProfile(UInt16 CardNo, UInt16 axis, ref UInt16 MasterType, ref UInt16 MasterIndex, ref UInt32 MasterEven, ref UInt32 SlaveEven, ref UInt32 MasterSlope);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_GearMove(UInt16 CardNo, UInt16 AxisNum, UInt16[] AxisList);
              
        //--------------------»ØÁãÔË¶¯---------------------
        //ÉèÖÃ¶ÁÈ¡HOMEĞÅºÅ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_home_pin_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_home_pin_logic(UInt16 CardNo, UInt16 axis, UInt16 org_logic, double filter);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_home_pin_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_home_pin_logic(UInt16 CardNo, UInt16 axis, ref UInt16 org_logic, ref double filter);
        //Éè¶¨¶ÁÈ¡Ö¸¶¨ÖáµÄ»ØÔ­µãÄ£Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_homemode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_homemode(UInt16 CardNo, UInt16 axis, UInt16 home_dir, double vel, UInt16 mode, UInt16 EZ_count);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_homemode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_homemode(UInt16 CardNo, UInt16 axis, ref UInt16 home_dir, ref double vel, ref UInt16 home_mode, ref UInt16 EZ_count);
        //ÉèÖÃ»ØÁãÓöÏŞÎ»ÊÇ·ñ·´ÕÒ£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_home_el_return(UInt16 CardNo, UInt16 axis, UInt16 enable);
        //¶ÁÈ¡²ÎÊıÓöÏŞÎ»·´ÕÒÊ¹ÄÜ£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_home_el_return(UInt16 CardNo, UInt16 axis, ref UInt16 enable);
        //Æô¶¯»ØÁã£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_home_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_home_move(UInt16 CardNo, UInt16 axis);
        //ÉèÖÃ¶ÁÈ¡»ØÁãËÙ¶È²ÎÊı£¨ÊÊÓÃÓÚRtex×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_home_profile_unit(ushort CardNo, ushort axis, double Low_Vel, double High_Vel, double Tacc, double Tdec);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_home_profile_unit(ushort CardNo, ushort axis, ref double Low_Vel, ref double High_Vel, ref double Tacc, ref double Tdec);
        //¶ÁÈ¡»ØÁãÖ´ĞĞ×´Ì¬£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_home_result(UInt16 CardNo, UInt16 axis, ref UInt16 state);
        //ÉèÖÃ¶ÁÈ¡»ØÁãÆ«ÒÆÁ¿¼°ÇåÁãÄ£Ê½£¨ÊÊÓÃÓÚDMC5X10Âö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_home_position_unit(UInt16 CardNo, UInt16 axis, UInt16 enable, double position);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_home_position_unit(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref double position);
        //£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_el_home(UInt16 CardNo, UInt16 axis, UInt16 mode);
        //»ØÁãÆ«ÒÆÄ£Ê½º¯Êı£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_home_shift_param(UInt16 CardNo, UInt16 axis, UInt16 pos_clear_mode, double ShiftValue);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_home_shift_param(UInt16 CardNo, UInt16 axis, ref UInt16 pos_clear_mode, ref double ShiftValue);
        //ÉèÖÃ»ØÁãÆ«ÒÆÁ¿¼°Æ«ÒÆÄ£Ê½£¨ÊÊÓÃÓÚDMC3000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_home_position(UInt16 CardNo, UInt16 axis, UInt16 enable, double position);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_home_position(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref double position);
        //ÉèÖÃ»ØÁãÏŞÎ»¾àÀë£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_home_soft_limit(UInt16 CardNo, UInt16 Axis, Int32 N_limit, Int32 P_limit);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_home_soft_limit(UInt16 CardNo, UInt16 Axis, ref Int32 N_limit, ref Int32 P_limit);
       
        //--------------------Ô­µãËø´æ-------------------
        //ÉèÖÃ¶ÁÈ¡EZËø´æÄ£Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_homelatch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_homelatch_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 logic, UInt16 source);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_homelatch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_homelatch_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 logic, ref UInt16 source);
        //¶ÁÈ¡Ô­µãËø´æ±êÖ¾£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_homelatch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_homelatch_flag(UInt16 CardNo, UInt16 axis);
        //Çå³ıÔ­µãËø´æ±êÖ¾£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_reset_homelatch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_reset_homelatch_flag(UInt16 CardNo, UInt16 axis);
        //¶ÁÈ¡Ô­µãËø´æÖµ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_homelatch_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 dmc_get_homelatch_value(UInt16 CardNo, UInt16 axis);
        //¶ÁÈ¡Ô­µãËø´æÖµ£¨unit£©£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_homelatch_value_unit(UInt16 CardNo, UInt16 axis, ref double pos);

        //--------------------EZËø´æ-------------------
        //ÉèÖÃ¶ÁÈ¡EZËø´æÄ£Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_ezlatch_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 logic, UInt16 source);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_ezlatch_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 logic, ref UInt16 source);
        //¶ÁÈ¡EZËø´æ±êÖ¾£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_ezlatch_flag(UInt16 CardNo, UInt16 axis);
        //Çå³ıEZËø´æ±êÖ¾£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_reset_ezlatch_flag(UInt16 CardNo, UInt16 axis);
        //¶ÁÈ¡EZËø´æÖµ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern Int32 dmc_get_ezlatch_value(UInt16 CardNo, UInt16 axis);
        //¶ÁÈ¡EZËø´æÖµ£¨unit£©£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_ezlatch_value_unit(UInt16 CardNo, UInt16 axis, ref double pos);

        //--------------------ÊÖÂÖÔË¶¯---------------------	
        //ÉèÖÃ¶ÁÈ¡ÊÖÂÖÍ¨µÀ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_handwheel_channel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_handwheel_channel(UInt16 CardNo, UInt16 index);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_handwheel_channel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_handwheel_channel(UInt16 CardNo, ref UInt16 index);
        //ÉèÖÃ¶ÁÈ¡µ¥ÖáÊÖÂÖÂö³åĞÅºÅµÄ¹¤×÷·½Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_handwheel_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_handwheel_inmode(UInt16 CardNo, UInt16 axis, UInt16 inmode, Int32 multi, double vh);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_handwheel_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_handwheel_inmode(UInt16 CardNo, UInt16 axis, ref UInt16 inmode, ref Int32 multi, ref double vh);
        //ÉèÖÃ¶ÁÈ¡µ¥ÖáÊÖÂÖÂö³åĞÅºÅµÄ¹¤×÷·½Ê½£¬¸¡µãĞÍ±¶ÂÊ£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_handwheel_inmode_decimals", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_handwheel_inmode_decimals(UInt16 CardNo, UInt16 axis, UInt16 inmode, double multi, double vh);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_handwheel_inmode_decimals", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_handwheel_inmode_decimals(UInt16 CardNo, UInt16 axis, ref UInt16 inmode, ref double multi, ref double vh);
        //ÉèÖÃ¶ÁÈ¡¶àÖáÊÖÂÖÂö³åĞÅºÅµÄ¹¤×÷·½Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_handwheel_inmode_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_handwheel_inmode_extern(UInt16 CardNo, UInt16 inmode, UInt16 AxisNum, UInt16[] AxisList, Int32[] multi);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_handwheel_inmode_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_handwheel_inmode_extern(UInt16 CardNo, ref UInt16 inmode, ref UInt16 AxisNum, UInt16[] AxisList, Int32[] multi);
        //ÉèÖÃ¶ÁÈ¡µ¥ÖáÊÖÂÖÂö³åĞÅºÅµÄ¹¤×÷·½Ê½£¬¸¡µãĞÍ±¶ÂÊ£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_handwheel_inmode_extern_decimals(UInt16 CardNo, UInt16 inmode, UInt16 AxisNum, UInt16[] AxisList, double[] multi);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_handwheel_inmode_extern_decimals(UInt16 CardNo, ref UInt16 inmode, ref UInt16 AxisNum, UInt16[] AxisList, double[] multi);
        //Æô¶¯ÊÖÂÖÔË¶¯£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_handwheel_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_handwheel_move(UInt16 CardNo, UInt16 axis);    
        //ÊÖÂÖÔË¶¯ ĞÂÔö×ÜÏßµÄÊÖÂÖÄ£Ê½  (±£Áô)
        [DllImport("LTDMC.dll")]
        public static extern short dmc_handwheel_set_axislist(UInt16 CardNo, UInt16 AxisSelIndex, UInt16 AxisNum, UInt16[] AxisList);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_handwheel_get_axislist(UInt16 CardNo, UInt16 AxisSelIndex, ref UInt16 AxisNum, UInt16[] AxisList);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_handwheel_set_ratiolist(UInt16 CardNo, UInt16 AxisSelIndex, UInt16 StartRatioIndex, UInt16 RatioSelNum, double[] RatioList);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_handwheel_get_ratiolist(UInt16 CardNo, UInt16 AxisSelIndex, UInt16 StartRatioIndex, UInt16 RatioSelNum, double[] RatioList);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_handwheel_set_mode(UInt16 CardNo, UInt16 InMode, UInt16 IfHardEnable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_handwheel_get_mode(UInt16 CardNo, ref UInt16 InMode, ref UInt16 IfHardEnable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_handwheel_set_index(UInt16 CardNo, UInt16 AxisSelIndex, UInt16 RatioSelIndex);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_handwheel_get_index(UInt16 CardNo, ref UInt16 AxisSelIndex, ref UInt16 RatioSelIndex);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_handwheel_stop(UInt16 CardNo);
        
        //-------------------------¸ßËÙËø´æ-------------------
        //ÉèÖÃ¶ÁÈ¡Ö¸¶¨ÖáµÄLTCĞÅºÅ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_ltc_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_ltc_mode(UInt16 CardNo, UInt16 axis, UInt16 ltc_logic, UInt16 ltc_mode, Double filter);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_ltc_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_ltc_mode(UInt16 CardNo, UInt16 axis, ref UInt16 ltc_logic, ref UInt16 ltc_mode, ref Double filter);
        //ÉèÖÃ¶Áµ½Ëø´æ·½Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_latch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_latch_mode(UInt16 CardNo, UInt16 axis, UInt16 all_enable, UInt16 latch_source, UInt16 triger_chunnel);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_latch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_latch_mode(UInt16 CardNo, UInt16 axis, ref UInt16 all_enable, ref UInt16 latch_source, ref UInt16 triger_chunnel);
        //¶ÁÈ¡±àÂëÆ÷Ëø´æÆ÷µÄÖµ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_latch_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 dmc_get_latch_value(UInt16 CardNo, UInt16 axis);
        //¶ÁÈ¡±àÂëÆ÷Ëø´æÆ÷µÄÖµunit£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_latch_value_unit(UInt16 CardNo, UInt16 axis, ref double pos_by_mm);
        //¶ÁÈ¡Ëø´æÆ÷±êÖ¾£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_latch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_latch_flag(UInt16 CardNo, UInt16 axis);
        //¸´Î»Ëø´æÆ÷±êÖ¾£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_reset_latch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_reset_latch_flag(UInt16 CardNo, UInt16 axis);
        //°´Ë÷ÒıÈ¡Öµ£¨ÊÊÓÃDMC3000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_latch_value_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 dmc_get_latch_value_extern(UInt16 CardNo, UInt16 axis, UInt16 Index);
        //¸ßËÙËø´æ£¨Ô¤Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_latch_value_extern_unit(UInt16 CardNo, UInt16 axis, UInt16 index, ref double pos_by_mm);//°´Ë÷ÒıÈ¡Öµ¶ÁÈ¡ 
        //¶ÁÈ¡Ëø´æ¸öÊı£¨ÊÊÓÃDMC3000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_latch_flag_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_latch_flag_extern(UInt16 CardNo, UInt16 axis);
        //ÉèÖÃ¶ÁÈ¡LTC·´ÏàÊä³ö£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_SetLtcOutMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_SetLtcOutMode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 bitno);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_GetLtcOutMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_GetLtcOutMode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 bitno);
        //LTC¶Ë¿Ú´¥·¢ÑÓÊ±¼±Í£Ê±¼ä µ¥Î»us£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_latch_stop_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_latch_stop_time(UInt16 CardNo, UInt16 axis, Int32 time);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_latch_stop_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_latch_stop_time(UInt16 CardNo, UInt16 axis, ref Int32 time);
        //ÉèÖÃ/»Ø¶ÁLTC¶Ë¿Ú´¥·¢ÑÓÊ±¼±Í£ÖáÅäÖÃ£¨ÊÊÓÃÓÚEtherCAT×ÜÏßÏµÁĞ¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_latch_stop_axis(ushort CardNo, ushort latch, ushort num, ushort[] axislist);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_latch_stop_axis(ushort CardNo, ushort latch, ref ushort num, ushort[] axislist);

        //----------------------¸ßËÙËø´æ ×ÜÏß¿¨---------------------------
        //ÅäÖÃËø´æÆ÷£ºËø´æÄ£Ê½0-µ¥´ÎËø´æ£¬1-Á¬ĞøËø´æ£»Ëø´æ±ßÑØ0-ÏÂ½µÑØ£¬1-ÉÏÉıÑØ£¬2-Ë«±ßÑØ£»ÂË²¨Ê±¼ä£¬µ¥Î»us£¨ÊÊÓÃÓÚËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_ltc_set_mode(ushort CardNo, ushort latch, ushort ltc_mode, ushort ltc_logic, double filter);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_ltc_get_mode(ushort CardNo, ushort latch, ref ushort ltc_mode, ref ushort ltc_logic, ref double filter);
        //ÅäÖÃËø´æÔ´£º0-Ö¸ÁîÎ»ÖÃ£¬1-±àÂëÆ÷·´À¡Î»ÖÃ£¨ÊÊÓÃÓÚËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_ltc_set_source(ushort CardNo, ushort latch, ushort axis, ushort ltc_source);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_ltc_get_source(ushort CardNo, ushort latch, ushort axis, ref ushort ltc_source);
        //¸´Î»Ëø´æÆ÷£¨ÊÊÓÃÓÚËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_ltc_reset(ushort CardNo, ushort latch);
        //¶ÁÈ¡Ëø´æ¸öÊı£¨ÊÊÓÃÓÚËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_ltc_get_number(ushort CardNo, ushort latch, ushort axis, ref int number);
        //¶ÁÈ¡Ëø´æÖµ£¨ÊÊÓÃÓÚËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_ltc_get_value_unit(ushort CardNo, ushort latch, ushort axis, ref double value);

        //-----------------------ÈíËø´æ ËùÓĞ¿¨---------------------------------
        //ÅäÖÃËø´æÆ÷£ºËø´æÄ£Ê½0-µ¥´ÎËø´æ£¬1-Á¬ĞøËø´æ£»Ëø´æ±ßÑØ0-ÏÂ½µÑØ£¬1-ÉÏÉıÑØ£¬2-Ë«±ßÑØ£»ÂË²¨Ê±¼ä£¬µ¥Î»us£¨ÊÊÓÃÓÚDMC5X10/3000ÏµÁĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_softltc_set_mode(ushort ConnectNo, ushort latch, ushort ltc_enable, ushort ltc_mode, ushort ltc_inbit, ushort ltc_logic, double filter);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_softltc_get_mode(ushort ConnectNo, ushort latch, ref ushort ltc_enable, ref ushort ltc_mode, ref ushort ltc_inbit, ref ushort ltc_logic, ref double filter);
        //ÅäÖÃËø´æÔ´£º0-Ö¸ÁîÎ»ÖÃ£¬1-±àÂëÆ÷·´À¡Î»ÖÃ£¨ÊÊÓÃÓÚDMC5X10/3000ÏµÁĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_softltc_set_source(ushort ConnectNo, ushort latch, ushort axis, ushort ltc_source);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_softltc_get_source(ushort ConnectNo, ushort latch, ushort axis, ref ushort ltc_source);
        //¸´Î»Ëø´æÆ÷£¨ÊÊÓÃÓÚDMC5X10/3000ÏµÁĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_softltc_reset(ushort ConnectNo, ushort latch);
        //¶ÁÈ¡Ëø´æ¸öÊı£¨ÊÊÓÃÓÚDMC5X10/3000ÏµÁĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_softltc_get_number(ushort ConnectNo, ushort latch, ushort axis, ref int number);
        //¶ÁÈ¡Ëø´æÖµ£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢ËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_softltc_get_value_unit(ushort ConnectNo, ushort latch, ushort axis, ref double value);

        //----------------------µ¥ÖáµÍËÙÎ»ÖÃ±È½Ï-----------------------	
        //ÅäÖÃ¶ÁÈ¡±È½ÏÆ÷£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_set_config(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 cmp_source);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_get_config(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 cmp_source);
        //Çå³ıËùÓĞ±È½Ïµã£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_clear_points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_clear_points(UInt16 CardNo, UInt16 axis);
        //Ìí¼Ó±È½Ïµã£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_add_point(UInt16 CardNo, UInt16 axis, int pos, UInt16 dir, UInt16 action, UInt32 actpara);
        //Ìí¼Ó±È½Ïµã£¨ÊÊÓÃÓÚËùÓĞDMC5X10Âö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_add_point_unit(UInt16 CardNo, UInt16 cmp, double pos, UInt16 dir, UInt16 action, UInt32 actpara);        
        //Ìí¼Ó±È½Ïµã£¨ÊÊÓÃÓÚE3032/R3032£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_add_point_cycle(UInt16 CardNo, UInt16 cmp, Int32 pos, UInt16 dir, UInt32 bitno, UInt32 cycle, UInt16 level);
        //Ìí¼Ó±È½Ïµãunit£¨ÊÊÓÃÓÚE5032£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_add_point_cycle_unit(UInt16 CardNo, UInt16 cmp, double pos, UInt16 dir, UInt32 bitno, UInt32 cycle, UInt16 level);
        //¶ÁÈ¡µ±Ç°±È½Ïµã£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢Rtex×ÜÏß¿¨¡¢E3032¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_get_current_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_get_current_point(UInt16 CardNo, UInt16 axis, ref Int32 pos);
        //¶ÁÈ¡µ±Ç°±È½Ïµã£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_get_current_point_unit(UInt16 CardNo, UInt16 cmp, ref double pos);
        //²éÑ¯ÒÑ¾­±È½Ï¹ıµÄµã£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_get_points_runned", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_get_points_runned(UInt16 CardNo, UInt16 axis, ref Int32 pointNum);
        //²éÑ¯¿ÉÒÔ¼ÓÈëµÄ±È½ÏµãÊıÁ¿£¨ÊÊÓÃÓÚËùÓĞÂö³å/×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_get_points_remained", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_get_points_remained(UInt16 CardNo, UInt16 axis, ref Int32 pointNum);
        
        //-------------------¶şÎ¬µÍËÙÎ»ÖÃ±È½Ï-----------------------
        //ÅäÖÃ¶ÁÈ¡±È½ÏÆ÷£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_set_config_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_set_config_extern(UInt16 CardNo, UInt16 enable, UInt16 cmp_source);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_get_config_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_get_config_extern(UInt16 CardNo, ref UInt16 enable, ref UInt16 cmp_source);
        //Çå³ıËùÓĞ±È½Ïµã£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_clear_points_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_clear_points_extern(UInt16 CardNo);
        //Ìí¼ÓÁ½ÖáÎ»ÖÃ±È½Ïµã£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_add_point_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_add_point_extern(UInt16 CardNo, UInt16[] axis, Int32[] pos, UInt16[] dir, UInt16 action, UInt32 actpara);
        //¶ÁÈ¡µ±Ç°±È½Ïµã£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_get_current_point_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_get_current_point_extern(UInt16 CardNo, Int32[] pos);
        //¶ÁÈ¡µ±Ç°±È½Ïµãunit£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_get_current_point_extern_unit(UInt16 CardNo, double[] pos);
        //Ìí¼ÓÁ½ÖáÎ»ÖÃ±È½Ïµã£¨ÊÊÓÃÓÚDMC5X10Âö³å¿¨£©      
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_add_point_extern_unit(UInt16 CardNo, UInt16[] axis, double[] pos, UInt16[] dir, UInt16 action, UInt32 actpara);
        //Ìí¼Ó¶şÎ¬µÍËÙÎ»ÖÃ±È½Ïµã£¨ÊÊÓÃÓÚEtherCAT×ÜÏßÏµÁĞ¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_add_point_cycle_2d(ushort CardNo, ushort[] axis, double[] pos, ushort[] dir, uint bitno, uint cycle, ushort level);
        //²éÑ¯ÒÑ¾­±È½Ï¹ıµÄµã£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_get_points_runned_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_get_points_runned_extern(UInt16 CardNo, ref Int32 pointNum);
        //²éÑ¯¿ÉÒÔ¼ÓÈëµÄ±È½ÏµãÊıÁ¿£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_compare_get_points_remained_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_compare_get_points_remained_extern(UInt16 CardNo, ref Int32 pointNum);
        //¶à×éÎ»ÖÃ±È½Ï£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_set_config_multi(UInt16 CardNo, UInt16 queue, UInt16 enable, UInt16 axis, UInt16 cmp_source);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_get_config_multi(UInt16 CardNo, UInt16 queue, ref UInt16 enable, ref UInt16 axis, ref UInt16 cmp_source);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_add_point_multi(UInt16 CardNo, UInt16 cmp, Int32 pos, UInt16 dir, UInt16 action, UInt32 actpara, double times);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_add_point_multi_unit(UInt16 CardNo, UInt16 cmp, double pos, UInt16 dir, UInt16 action, UInt32 actpara, double times);//Ìí¼Ó±È½Ïµã ÔöÇ¿
        
        //----------- µ¥Öá¸ßËÙÎ»ÖÃ±È½Ï-----------------------        
        //ÉèÖÃ¶ÁÈ¡¸ßËÙ±È½ÏÄ£Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_hcmp_set_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_hcmp_set_mode(UInt16 CardNo, UInt16 hcmp, UInt16 cmp_enable);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_hcmp_get_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_hcmp_get_mode(UInt16 CardNo, UInt16 hcmp, ref UInt16 cmp_enable);
        //ÉèÖÃ¸ßËÙ±È½Ï²ÎÊı£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_hcmp_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_hcmp_set_config(UInt16 CardNo, UInt16 hcmp, UInt16 axis, UInt16 cmp_source, UInt16 cmp_logic, Int32 time);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_hcmp_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_hcmp_get_config(UInt16 CardNo, UInt16 hcmp, ref UInt16 axis, ref UInt16 cmp_source, ref UInt16 cmp_logic, ref Int32 time);
        //¸ßËÙ±È½ÏÄ£Ê½À©Õ¹£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_set_config_extern(UInt16 CardNo, UInt16 hcmp, UInt16 axis, UInt16 cmp_source, UInt16 cmp_logic, UInt16 cmp_mode, Int32 dist, Int32 time);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_get_config_extern(UInt16 CardNo, UInt16 hcmp, ref UInt16 axis, ref UInt16 cmp_source, ref UInt16 cmp_logic, ref UInt16 cmp_mode, ref Int32 dist, ref Int32 time);
        //Ìí¼Ó±È½Ïµã£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢E3032×ÜÏß¿¨¡¢R3032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_hcmp_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_hcmp_add_point(UInt16 CardNo, UInt16 hcmp, Int32 cmp_pos);
        //Ìí¼Ó±È½Ïµãunit£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_add_point_unit(UInt16 CardNo, UInt16 hcmp, double cmp_pos);       
        //ÉèÖÃ¶ÁÈ¡ÏßĞÔÄ£Ê½²ÎÊı£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢E3032×ÜÏß¿¨¡¢R3032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_hcmp_set_liner", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_hcmp_set_liner(UInt16 CardNo, UInt16 hcmp, Int32 Increment, Int32 Count);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_hcmp_get_liner", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_hcmp_get_liner(UInt16 CardNo, UInt16 hcmp, ref Int32 Increment, ref Int32 Count);
        //ÉèÖÃÏßĞÔÄ£Ê½²ÎÊı£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_set_liner_unit(UInt16 CardNo, UInt16 hcmp, double Increment, Int32 Count);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_get_liner_unit(UInt16 CardNo, UInt16 hcmp, ref double Increment, ref Int32 Count);
        //¶ÁÈ¡¸ßËÙ±È½Ï×´Ì¬£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢E3032×ÜÏß¿¨¡¢R3032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_hcmp_get_current_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_hcmp_get_current_state(UInt16 CardNo, UInt16 hcmp, ref Int32 remained_points, ref Int32 current_point, ref Int32 runned_points);
        //¶ÁÈ¡¸ßËÙ±È½Ï×´Ì¬£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_get_current_state_unit(UInt16 CardNo, UInt16 hcmp, ref Int32 remained_points, ref double current_point, ref Int32 runned_points); //¶ÁÈ¡¸ßËÙ±È½Ï×´Ì¬
        //Çå³ı±È½Ïµã£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_hcmp_clear_points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_hcmp_clear_points(UInt16 CardNo, UInt16 hcmp);
        //¶ÁÈ¡Ö¸¶¨CMP¶Ë¿ÚµÄµçÆ½£¨±£Áô£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_cmp_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_cmp_pin(UInt16 CardNo, UInt16 hcmp);
        //¿ØÖÆcmp¶Ë¿ÚÊä³ö£¨±£Áô£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_write_cmp_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_cmp_pin(UInt16 CardNo, UInt16 hcmp, UInt16 on_off);
        //1¡¢	ÆôÓÃ»º´æ·½Ê½Ìí¼Ó±È½ÏÎ»ÖÃ£º£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_fifo_set_mode(UInt16 CardNo, UInt16 hcmp, UInt16 fifo_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_fifo_get_mode(UInt16 CardNo, UInt16 hcmp, ref UInt16 fifo_mode);
        //2¡¢	¶ÁÈ¡Ê£Óà»º´æ×´Ì¬£¬ÉÏÎ»»úÍ¨¹ı´Ëº¯ÊıÅĞ¶ÏÊÇ·ñ¼ÌĞøÌí¼Ó±È½ÏÎ»ÖÃ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_fifo_get_state(UInt16 CardNo, UInt16 hcmp, ref long remained_points);
        //3¡¢	°´Êı×éµÄ·½Ê½ÅúÁ¿Ìí¼Ó±È½ÏÎ»ÖÃ£¨ÊÊÓÃÓÚDMC5000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_fifo_add_point_unit(UInt16 CardNo, UInt16 hcmp, UInt16 num, double[] cmp_pos);
        //4¡¢	Çå³ı±È½ÏÎ»ÖÃ,Ò²»á°ÑFPGAµÄÎ»ÖÃÍ¬²½Çå³ıµô£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_fifo_clear_points(UInt16 CardNo, UInt16 hcmp);
        //Ìí¼Ó´óÊı¾İ£¬»á¶ÂÈûÒ»¶ÎÊ±¼ä£¬Ö¸µ¼Êı¾İÌí¼ÓÍê³É£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_fifo_add_table(UInt16 CardNo, UInt16 hcmp, UInt16 num, double[] cmp_pos);
        //Ò»Î¬¸ßËÙ±È½Ï£¬¶ÓÁĞÄ£Ê½Ìí¼ÓµÄ±È½Ïµã¹ØÁªÔË¶¯·½Ïò£¬Ìí¼ÓÉÙÁ¿Êı¾İ£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_fifo_add_point_dir_unit(ushort CardNo, ushort hcmp, ushort num, double[] cmp_pos, uint dir);
        //Ò»Î¬¸ßËÙ±È½Ï£¬¶ÓÁĞÄ£Ê½Ìí¼ÓµÄ±È½Ïµã¹ØÁªÔË¶¯·½Ïò£¬Ìí¼Ó´óÁ¿Êı¾İ£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_fifo_add_table_dir(ushort CardNo, ushort hcmp, ushort num, double[] cmp_pos, uint dir);
        //----------- ¶şÎ¬¸ßËÙÎ»ÖÃ±È½Ï-----------------------        
        //ÉèÖÃ¶ÁÈ¡¸ßËÙ±È½ÏÊ¹ÄÜ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_set_enable(UInt16 CardNo, UInt16 hcmp, UInt16 cmp_enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_get_enable(UInt16 CardNo, UInt16 hcmp, ref UInt16 cmp_enable);
        //ÅäÖÃ¶ÁÈ¡¶şÎ¬¸ßËÙ±È½ÏÆ÷£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_set_config(UInt16 CardNo, UInt16 hcmp, UInt16 cmp_mode, UInt16 x_axis, UInt16 x_cmp_source, UInt16 y_axis, UInt16 y_cmp_source, Int32 error, UInt16 cmp_logic, Int32 time, UInt16
 pwm_enable, double duty, Int32 freq, UInt16 port_sel, UInt16 pwm_number);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_get_config(UInt16 CardNo, UInt16 hcmp, ref UInt16 cmp_mode, ref UInt16 x_axis, ref UInt16 x_cmp_source, ref UInt16 y_axis, ref UInt16 y_cmp_source, ref Int32 error, ref UInt16 
cmp_logic, ref Int32 time, ref UInt16 pwm_enable, ref double duty, ref Int32 freq, ref UInt16 port_sel, ref UInt16 pwm_number);
        //ÅäÖÃ¶ÁÈ¡¶şÎ¬¸ßËÙ±È½ÏÆ÷£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_set_config_unit(UInt16 CardNo, UInt16 hcmp, UInt16 cmp_mode, UInt16 x_axis, UInt16 x_cmp_source, double x_cmp_error, UInt16 y_axis, UInt16 y_cmp_source, double y_cmp_error, 
UInt16 cmp_logic, int time);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_get_config_unit(UInt16 CardNo, UInt16 hcmp, ref UInt16 cmp_mode, ref UInt16 x_axis, ref UInt16 x_cmp_source, ref double x_cmp_error, ref UInt16 y_axis, ref UInt16 y_cmp_source, 
ref double y_cmp_error, ref UInt16 cmp_logic, ref int time);
        //Ìí¼Ó¶şÎ¬¸ßËÙÎ»ÖÃ±È½Ïµã£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_add_point(UInt16 CardNo, UInt16 hcmp, Int32 x_cmp_pos, Int32 y_cmp_pos);
        //Ìí¼Ó¶şÎ¬¸ßËÙÎ»ÖÃ±È½Ïµãunit£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_add_point_unit(UInt16 CardNo, UInt16 hcmp, double x_cmp_pos, double y_cmp_pos, UInt16 cmp_outbit);
        //¶ÁÈ¡¶şÎ¬¸ßËÙ±È½Ï²ÎÊı£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_get_current_state(UInt16 CardNo, UInt16 hcmp, ref Int32 remained_points, ref Int32 x_current_point, ref Int32 y_current_point, ref Int32 runned_points, ref UInt16 current_state
);
        //¶ÁÈ¡¶şÎ¬¸ßËÙ±È½Ï²ÎÊı£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_get_current_state_unit(UInt16 CardNo, UInt16 hcmp, ref int remained_points, ref double x_current_point, ref double y_current_point, ref int runned_points, ref UInt16 
current_state, ref UInt16 current_outbit); 
        //Çå³ı¶şÎ¬¸ßËÙÎ»ÖÃ±È½Ïµã£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_clear_points(UInt16 CardNo, UInt16 hcmp);
        //Ç¿ÖÆ¶şÎ¬¸ßËÙ±È½ÏÊä³ö£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_force_output(UInt16 CardNo, UInt16 hcmp, UInt16 cmp_outbit);
        //ÅäÖÃ¶ÁÈ¡¶şÎ¬±È½ÏPWMÊä³öÄ£Ê½£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_set_pwmoutput(UInt16 CardNo, UInt16 hcmp, UInt16 pwm_enable, double duty, double freq, UInt16 pwm_number);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_get_pwmoutput(UInt16 CardNo, UInt16 hcmp, ref UInt16 pwm_enable, ref double duty, ref double freq, ref UInt16 pwm_number);
        
        //------------------------Í¨ÓÃIO-----------------------
        //¶ÁÈ¡ÊäÈë¿ÚµÄ×´Ì¬£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_inbit(UInt16 CardNo, UInt16 bitno);
        //ÉèÖÃÊä³ö¿ÚµÄ×´Ì¬£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_outbit(UInt16 CardNo, UInt16 bitno, UInt16 on_off);
        //¶ÁÈ¡Êä³ö¿ÚµÄ×´Ì¬£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_outbit(UInt16 CardNo, UInt16 bitno);
        //¶ÁÈ¡ÊäÈë¶Ë¿ÚµÄÖµ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_inport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 dmc_read_inport(UInt16 CardNo, UInt16 portno);
        //¶ÁÈ¡Êä³ö¶Ë¿ÚµÄÖµ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 dmc_read_outport(UInt16 CardNo, UInt16 portno);
        //ÉèÖÃËùÓĞÊä³ö¶Ë¿ÚµÄÖµ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_write_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_outport(UInt16 CardNo, UInt16 portno, UInt32 outport_val);
        //ÉèÖÃÍ¨ÓÃÊä³ö¶Ë¿ÚµÄÖµ£¨±£Áô£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_write_outport_16X", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_outport_16X(UInt16 CardNo, UInt16 portno, UInt32 outport_val);
        //---------------------------Í¨ÓÃIO´ø·µ»ØÖµ¼ì²â----------------------
        //¶ÁÈ¡ÊäÈë¿ÚµÄ×´Ì¬£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_inbit_ex(ushort CardNo, ushort bitno, ref ushort state);
        //¶ÁÈ¡Êä³ö¿ÚµÄ×´Ì¬£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_outbit_ex(ushort CardNo, ushort bitno, ref ushort state);
        //¶ÁÈ¡ÊäÈë¶Ë¿ÚµÄÖµ£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_inport_ex(ushort CardNo, ushort portno, ref UInt32 state);
        //¶ÁÈ¡Êä³ö¶Ë¿ÚµÄÖµ£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_outport_ex(ushort CardNo, ushort portno, ref UInt32 state);
        
        //ÉèÖÃ¶ÁÈ¡ĞéÄâIOÓ³Éä¹ØÏµ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£© 
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_io_map_virtual", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_io_map_virtual(UInt16 CardNo, UInt16 bitno, UInt16 MapIoType, UInt16 MapIoIndex, double Filter);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_io_map_virtual", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_io_map_virtual(UInt16 CardNo, UInt16 bitno, ref UInt16 MapIoType, ref UInt16 MapIoIndex, ref double Filter);
        //¶ÁÈ¡ĞéÄâÊäÈë¿ÚµÄ×´Ì¬£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_inbit_virtual", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_inbit_virtual(UInt16 CardNo, UInt16 bitno);
        //IOÑÓÊ±·­×ª£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_reverse_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_reverse_outbit(UInt16 CardNo, UInt16 bitno, double reverse_time);
        //ÉèÖÃ¶ÁÈ¡IO¼ÆÊıÄ£Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_io_count_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_io_count_mode(UInt16 CardNo, UInt16 bitno, UInt16 mode, double filter_time);        
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_io_count_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_io_count_mode(UInt16 CardNo, UInt16 bitno, ref UInt16 mode, ref double filter_time);
        //ÉèÖÃIO¼ÆÊıÖµ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_io_count_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_io_count_value(UInt16 CardNo, UInt16 bitno, UInt32 CountValue);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_io_count_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_io_count_value(UInt16 CardNo, UInt16 bitno, ref UInt32 CountValue);
                 
        //-----------------------×¨ÓÃIO Âö³å¿¨×¨ÓÃ-------------------------
        //ÉèÖÃ¶ÁÈ¡ÖáIOÓ³Éä¹ØÏµ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_axis_io_map", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_axis_io_map(UInt16 CardNo, UInt16 Axis, UInt16 IoType, UInt16 MapIoType, UInt16 MapIoIndex, double Filter);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_axis_io_map", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_axis_io_map(UInt16 CardNo, UInt16 Axis, UInt16 IoType, ref UInt16 MapIoType, ref UInt16 MapIoIndex, ref double Filter);
        //ÉèÖÃËùÓĞ×¨ÓÃIOÂË²¨Ê±¼ä£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_special_input_filter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_special_input_filter(UInt16 CardNo, double Filter);
        // »ØÔ­µã¼õËÙĞÅºÅÅäÖÃ£¬(DMC3410×¨ÓÃ)
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_sd_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_sd_mode(UInt16 CardNo, UInt16 axis, UInt16 sd_logic, UInt16 sd_mode);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_sd_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_sd_mode(UInt16 CardNo, UInt16 axis, ref UInt16 sd_logic, ref UInt16 sd_mode);
        //ÉèÖÃ¶ÁÈ¡INPĞÅºÅ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_inp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_inp_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 inp_logic);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_inp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_inp_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 inp_logic);
        //ÉèÖÃ¶ÁÈ¡RDYĞÅºÅ£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_rdy_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 rdy_logic);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_rdy_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 rdy_logic);
        //ÉèÖÃ¶ÁÈ¡ERCĞÅºÅ£¨±£Áô£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_erc_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_erc_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 erc_logic, UInt16 erc_width, UInt16 erc_off_time);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_erc_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_erc_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 erc_logic, ref UInt16 erc_width, ref UInt16 erc_off_time);
        //ÉèÖÃ¶ÁÈ¡ALMĞÅºÅ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_alm_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_alm_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 alm_logic, UInt16 alm_action);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_alm_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_alm_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 alm_logic, ref UInt16 alm_action);
        //ÉèÖÃ¶ÁÈ¡EZĞÅºÅ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_ez_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_ez_mode(UInt16 CardNo, UInt16 axis, UInt16 ez_logic, UInt16 ez_mode, double filter);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_ez_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_ez_mode(UInt16 CardNo, UInt16 axis, ref UInt16 ez_logic, ref UInt16 ez_mode, ref double filter);
        //Êä³ö¶ÁÈ¡SEVONĞÅºÅ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_write_sevon_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_sevon_pin(UInt16 CardNo, UInt16 axis, UInt16 on_off);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_sevon_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_sevon_pin(UInt16 CardNo, UInt16 axis);
        //¿ØÖÆERCĞÅºÅÊä³ö£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_write_erc_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_erc_pin(UInt16 CardNo, UInt16 axis, UInt16 sel);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_erc_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_erc_pin(UInt16 CardNo, UInt16 axis);
        //¶ÁÈ¡RDY×´Ì¬£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_rdy_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_rdy_pin(UInt16 CardNo, UInt16 axis);
        //Êä³öËÅ·ş¸´Î»ĞÅºÅ£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_write_sevrst_pin(UInt16 CardNo, UInt16 axis, UInt16 on_off);
        //¶ÁËÅ·ş¸´Î»ĞÅºÅ£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_sevrst_pin(UInt16 CardNo, UInt16 axis);

        //---------------------±àÂëÆ÷ Âö³å¿¨---------------------
        //Éè¶¨¶ÁÈ¡±àÂëÆ÷µÄ¼ÆÊı·½Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_counter_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_counter_inmode(UInt16 CardNo, UInt16 axis, UInt16 mode);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_counter_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_counter_inmode(UInt16 CardNo, UInt16 axis, ref UInt16 mode);
        //±àÂëÆ÷Öµ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 dmc_get_encoder(UInt16 CardNo, UInt16 axis);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_encoder(UInt16 CardNo, UInt16 axis, Int32 encoder_value);
        //±àÂëÆ÷Öµ(µ±Á¿)£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢ËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_encoder_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_encoder_unit(UInt16 CardNo, UInt16 axis, double pos);     //µ±Ç°·´À¡Î»ÖÃ
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_encoder_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_encoder_unit(UInt16 CardNo, UInt16 axis, ref double pos);        
        //---------------------¸¨Öú±àÂëÆ÷ ×ÜÏß¿¨---------------------
        //ÊÖÂÖ±àÂëÆ÷£¨±¸ÓÃ£¬Í¬dmc_set_extra_encoder£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_handwheel_encoder(ushort CardNo, ushort channel, int pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_handwheel_encoder(ushort CardNo, ushort channel, ref int pos);
        //ÉèÖÃ¸¨Öú±àÂëÄ£Ê½£¨ÊÊÓÃÓÚËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_extra_encoder_mode(ushort CardNo, ushort channel, ushort inmode, ushort multi);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_extra_encoder_mode(ushort CardNo, ushort channel, ref ushort inmode, ref ushort multi);
        //ÉèÖÃ¸¨Öú±àÂëÆ÷Öµ£¨ÊÊÓÃÓÚËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_extra_encoder(ushort CardNo, ushort channel, int pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_extra_encoder(ushort CardNo, ushort channel, ref int pos);
        //---------------------Î»ÖÃ¼ÆÊı¿ØÖÆ---------------------
        //µ±Ç°Î»ÖÃ(µ±Á¿)£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢ËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_position_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_position_unit(UInt16 CardNo, UInt16 axis, double pos);   //µ±Ç°Ö¸ÁîÎ»ÖÃ
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_position_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_position_unit(UInt16 CardNo, UInt16 axis, ref double pos);
        //µ±Ç°Î»ÖÃ(Âö³å)£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 dmc_get_position(UInt16 CardNo, UInt16 axis);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_position(UInt16 CardNo, UInt16 axis, Int32 current_position);
        //--------------------ÔË¶¯×´Ì¬----------------------	
        //¶ÁÈ¡Ö¸¶¨ÖáµÄµ±Ç°ËÙ¶È£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_current_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double dmc_read_current_speed(UInt16 CardNo, UInt16 axis);
        //¶ÁÈ¡µ±Ç°ËÙ¶È(µ±Á¿)£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢ËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_current_speed_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_current_speed_unit(UInt16 CardNo, UInt16 Axis, ref double current_speed);   //Öáµ±Ç°ÔËĞĞËÙ¶È
        //¶ÁÈ¡µ±Ç°¿¨µÄ²å²¹ËÙ¶È£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢ËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_vector_speed_unit(UInt16 CardNo, UInt16 Crd, ref double current_speed);	//¶ÁÈ¡µ±Ç°¿¨µÄ²å²¹ËÙ¶È
        //¶ÁÈ¡Ö¸¶¨ÖáµÄÄ¿±êÎ»ÖÃ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢R3032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 dmc_get_target_position(UInt16 CardNo, UInt16 axis);
        //¶ÁÈ¡Ö¸¶¨ÖáµÄÄ¿±êÎ»ÖÃ(µ±Á¿)£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢ËùÓĞEtherCAT×ÜÏßÏµÁĞ¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_target_position_unit(UInt16 CardNo, UInt16 axis, ref double pos);
        //¶ÁÈ¡Ö¸¶¨ÖáµÄÔË¶¯×´Ì¬£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_check_done(UInt16 CardNo, UInt16 axis);
        //¶ÁÈ¡Ö¸¶¨ÖáµÄÔË¶¯×´Ì¬£¨ÊÊÓÃÓÚËùÓĞ¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_check_done_ex(ushort CardNo, ushort axis, ref ushort state);
        //²å²¹ÔË¶¯×´Ì¬£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_check_done_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_check_done_multicoor(UInt16 CardNo, UInt16 crd);
        //¶ÁÈ¡Ö¸¶¨ÖáÓĞ¹ØÔË¶¯ĞÅºÅµÄ×´Ì¬£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_axis_io_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 dmc_axis_io_status(UInt16 CardNo, UInt16 axis);
        //¶ÁÈ¡Ö¸¶¨ÖáÓĞ¹ØÔË¶¯ĞÅºÅµÄ×´Ì¬£¨ÊÊÓÃÓÚËùÓĞ¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_axis_io_status_ex(ushort CardNo, ushort axis,  uint[] state);
        //µ¥ÖáÍ£Ö¹£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_stop(UInt16 CardNo, UInt16 axis, UInt16 stop_mode);
        //Í£Ö¹²å²¹Æ÷£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_stop_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_stop_multicoor(UInt16 CardNo, UInt16 crd, UInt16 stop_mode);
        //½ô¼±Í£Ö¹ËùÓĞÖá£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_emg_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_emg_stop(UInt16 CardNo);
        //Âö³å¿¨Ö¸Áî Ö÷¿¨Óë½ÓÏßºĞÍ¨Ñ¶×´Ì¬£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_LinkState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_LinkState(UInt16 CardNo, ref UInt16 State);
        //¶ÁÈ¡Ö¸¶¨ÖáµÄÔË¶¯Ä£Ê½£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢ËùÓĞ×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_axis_run_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_axis_run_mode(UInt16 CardNo, UInt16 axis, ref UInt16 run_mode);  
        //¶ÁÈ¡ÖáÍ£Ö¹Ô­Òò£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_stop_reason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_stop_reason(UInt16 CardNo, UInt16 axis, ref Int32 StopReason);    
        //Çå³ıÖáÍ£Ö¹Ô­Òò£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_clear_stop_reason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_clear_stop_reason(UInt16 CardNo, UInt16 axis);
        //trace¹¦ÄÜ£¨ÄÚ²¿Ê¹ÓÃº¯Êı£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_trace", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_trace(UInt16 CardNo, UInt16 axis, UInt16 enable);   //trace¹¦ÄÜ
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_trace", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_trace(UInt16 CardNo, UInt16 axis, ref UInt16 enable);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_trace_data", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_trace_data(UInt16 CardNo, UInt16 axis, UInt16 data_option, ref Int32 ReceiveSize, double[] time, double[] data, ref Int32 remain_num);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_start(ushort CardNo, ushort AxisNum, ushort[] AxisList);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_stop(ushort CardNo);

        //»¡³¤¼ÆËã£¨±¸ÓÃ£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_calculate_arclength_center", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_calculate_arclength_center(double[] start_pos, double[] target_pos, double[] cen_pos, UInt16 arc_dir, double circle, ref double ArcLength);      //¼ÆËãÔ²ĞÄÔ²»¡»¡³¤
        [DllImport("LTDMC.dll", EntryPoint = "dmc_calculate_arclength_3point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_calculate_arclength_3point(double[] start_pos, double[] mid_pos, double[] target_pos, double circle, ref double ArcLength);      //¼ÆËãÈıµãÔ²»¡»¡³¤
        [DllImport("LTDMC.dll", EntryPoint = "dmc_calculate_arclength_radius", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_calculate_arclength_radius(double[] start_pos, double[] target_pos, double arc_radius, UInt16 arc_dir, double circle, ref double ArcLength);     //¼ÆËã°ë¾¶Ô²»¡»¡³¤

        //--------------------CAN-IOÀ©Õ¹----------------------	
        //CAN-IOÀ©Õ¹£¬¾É½Ó¿Úº¯Êı£¨±£Áô£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_can_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_can_state(UInt16 CardNo, UInt16 NodeNum, UInt16 state, UInt16 Baud);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_can_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_can_state(UInt16 CardNo, ref UInt16 NodeNum, ref UInt16 state);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_write_can_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_can_outbit(UInt16 CardNo, UInt16 Node, UInt16 bitno, UInt16 on_off);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_can_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_can_outbit(UInt16 CardNo, UInt16 Node, UInt16 bitno);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_can_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_can_inbit(UInt16 CardNo, UInt16 Node, UInt16 bitno);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_write_can_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_can_outport(UInt16 CardNo, UInt16 Node, UInt16 PortNo, UInt32 outport_val);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_can_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 dmc_read_can_outport(UInt16 CardNo, UInt16 Node, UInt16 PortNo);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_read_can_inport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 dmc_read_can_inport(UInt16 CardNo, UInt16 Node, UInt16 PortNo);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_can_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_can_errcode(UInt16 CardNo, ref UInt16 Errcode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_can_errcode_extern(UInt16 CardNo, ref UInt16 Errcode, ref UInt16 msg_losed, ref UInt16 emg_msg_num, ref UInt16 lostHeartB, ref UInt16 EmgMsg);
        //ÉèÖÃCAN ioÊä³ö£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_outbit(ushort CardNo, ushort NodeID, ushort IoBit, ushort IoValue);
        //¶ÁÈ¡CAN ioÊä³ö£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_outbit(ushort CardNo, ushort NodeID, ushort IoBit, ref ushort IoValue);
        //¶ÁÈ¡CAN ioÊäÈë£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_inbit(ushort CardNo, ushort NodeID, ushort IoBit, ref ushort IoValue);
        //ÉèÖÃCAN ioÊä³ö32Î»£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_outport(ushort CardNo, ushort NodeID, ushort PortNo, UInt32 IoValue);
        //¶ÁÈ¡CAN ioÊä³ö32Î»£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_outport(ushort CardNo, ushort NodeID, ushort PortNo, ref UInt32 IoValue);
        //¶ÁÈ¡CAN ioÊäÈë32Î»£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_inport(ushort CardNo, ushort NodeID, ushort PortNo, ref UInt32 IoValue);
        //---------------------------CAN IO´ø·µ»ØÖµ¼ì²â----------------------
        //ÉèÖÃCAN ioÊä³ö£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_outbit_ex(ushort CardNo, ushort NoteID, ushort IoBit, ushort IoValue, ref ushort state);
        //¶ÁÈ¡CAN ioÊä³ö£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_outbit_ex(ushort CardNo, ushort NoteID, ushort IoBit, ref ushort IoValue, ref ushort state);
        //¶ÁÈ¡CAN ioÊäÈë£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_inbit_ex(ushort CardNo, ushort NoteID, ushort IoBit, ref ushort IoValue, ref ushort state);
        //ÉèÖÃCAN ioÊä³ö32Î»£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_outport_ex(ushort CardNo, ushort NoteID, ushort portno, UInt32 outport_val, ref ushort state);
        //¶ÁÈ¡CAN ioÊä³ö32Î»£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_outport_ex(ushort CardNo, ushort NoteID, ushort portno, ref UInt32 outport_val, ref ushort state);
        //¶ÁÈ¡CAN ioÊäÈë32Î»£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_inport_ex(ushort CardNo, ushort NoteID, ushort portno, ref UInt32 inport_val, ref ushort state);
        //---------------------------CAN ADDA----------------------
        //CAN ADDAÖ¸Áî ÉèÖÃDA²ÎÊı £¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_da_output(ushort CardNo, ushort NoteID, ushort channel, double Value);
        //¶ÁÈ¡CAN DA²ÎÊı£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_da_output(ushort CardNo, ushort NoteID, ushort channel, ref double Value);
        //¶ÁÈ¡CAN AD²ÎÊı£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_ad_input(ushort CardNo, ushort NoteID, ushort channel, ref double Value);
        //ÅäÖÃCAN ADÄ£Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_ad_mode(ushort CardNo, ushort NoteID, ushort channel, ushort mode, uint buffer_nums);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_ad_mode(ushort CardNo, ushort NoteID, ushort channel, ref ushort mode, uint buffer_nums);
        //ÅäÖÃCAN DAÄ£Ê½£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_da_mode(ushort CardNo, ushort NoteID, ushort channel, ushort mode, uint buffer_nums);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_da_mode(ushort CardNo, ushort NoteID, ushort channel, ref ushort mode, uint buffer_nums);
        //CAN²ÎÊıĞ´Èëflash£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_to_flash(ushort CardNo, ushort PortNum, ushort NodeNum);
        //CAN×ÜÏßÁ´½Ó£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_connect_state(UInt16 CardNo, UInt16 NodeNum, UInt16 state, UInt16 baud);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_connect_state(UInt16 CardNo, ref UInt16 NodeNum, ref UInt16 state);
        //---------------------------CAN ADDA´ø·µ»ØÖµ¼ì²â----------------------
        //ÉèÖÃCAN DA²ÎÊı£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_da_output_ex(ushort CardNo, ushort NoteID, ushort channel, double Value, ref ushort state);
        //¶ÁÈ¡CAN DA²ÎÊı£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_da_output_ex(ushort CardNo, ushort NoteID, ushort channel, ref double Value, ref ushort state);
        //¶ÁÈ¡CAN AD²ÎÊı£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_ad_input_ex(ushort CardNo, ushort NoteID, ushort channel, ref double Value, ref ushort state);
        //ÅäÖÃCAN ADÄ£Ê½£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_ad_mode_ex(ushort CardNo, ushort NoteID, ushort channel, ushort mode, UInt32 buffer_nums, ref ushort state);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_ad_mode_ex(ushort CardNo, ushort NoteID, ushort channel, ref ushort mode, UInt32 buffer_nums, ref ushort state);
        //ÅäÖÃCAN DAÄ£Ê½£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_da_mode_ex(ushort CardNo, ushort NoteID, ushort channel, ushort mode, UInt32 buffer_nums, ref ushort state);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_da_mode_ex(ushort CardNo, ushort NoteID, ushort channel, ref ushort mode, UInt32 buffer_nums, ref ushort state);
        //²ÎÊıĞ´Èëflash£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_to_flash_ex(ushort CardNo, ushort PortNum, ushort NodeNum, ref ushort state);

        //--------------------Á¬Ğø²å²¹º¯Êı----------------------	
        //´ò¿ªÁ¬Ğø»º´æÇø£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_open_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_open_list(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList);
        //¹Ø±ÕÁ¬Ğø»º´æÇø£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_close_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_close_list(UInt16 CardNo, UInt16 Crd);
        //¸´Î»Á¬Ğø»º´æÇø£¨Ô¤Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_reset_list(UInt16 CardNo, UInt16 Crd);
        //Á¬Ğø²å²¹ÖĞÍ£Ö¹£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_stop_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_stop_list(UInt16 CardNo, UInt16 Crd, UInt16 stop_mode);
        //Á¬Ğø²å²¹ÖĞÔİÍ££¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_pause_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_pause_list(UInt16 CardNo, UInt16 Crd);
        //¿ªÊ¼Á¬Ğø²å²¹£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_start_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_start_list(UInt16 CardNo, UInt16 Crd);
        //¼ì²âÁ¬Ğø²å²¹ÔË¶¯×´Ì¬£º0-ÔËĞĞ£¬1-ÔİÍ££¬2-Õı³£Í£Ö¹£¨DMC5X10²»Ö§³Ö£©£¬3-Î´Æô¶¯£¬4-¿ÕÏĞ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_get_run_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_get_run_state(UInt16 CardNo, UInt16 Crd);
        //¼ì²âÁ¬Ğø²å²¹ÔË¶¯×´Ì¬£º0-ÔËĞĞ£¬1-Í£Ö¹£¨Ô¤Áô£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_check_done(UInt16 CardNo, UInt16 Crd);  
        //²éÁ¬Ğø²å²¹Ê£Óà»º´æÊı£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_remain_space", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 dmc_conti_remain_space(UInt16 CardNo, UInt16 Crd);
        //¶ÁÈ¡µ±Ç°Á¬Ğø²å²¹¶ÎµÄ±êºÅ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_read_current_mark", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 dmc_conti_read_current_mark(UInt16 CardNo, UInt16 Crd);
        //blend¹Õ½Ç¹ı¶ÈÄ£Ê½£¨ÊÊÓÃÓÚDMC5000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_set_blend", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_set_blend(UInt16 CardNo, UInt16 Crd, UInt16 enable);      
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_get_blend", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_get_blend(UInt16 CardNo, UInt16 Crd, ref UInt16 enable);
        //ÉèÖÃÃ¿¶ÎËÙ¶È±ÈÀı  »º³åÇøÖ¸Áî£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_set_override", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_set_override(UInt16 CardNo, UInt16 Crd, double Percent);      
        //ÉèÖÃ²å²¹ÖĞ¶¯Ì¬±äËÙ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_change_speed_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_change_speed_ratio(UInt16 CardNo, UInt16 Crd, double Percent);
        //Ğ¡Ïß¶ÎÇ°Õ°£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_set_lookahead_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_set_lookahead_mode(UInt16 CardNo, UInt16 Crd, UInt16 enable, Int32 LookaheadSegments, double PathError, double LookaheadAcc);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_get_lookahead_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_get_lookahead_mode(UInt16 CardNo, UInt16 Crd, ref UInt16 enable, ref Int32 LookaheadSegments, ref double PathError, ref double LookaheadAcc);
        //--------------------Á¬Ğø²å²¹IO¹¦ÄÜ----------------------
        //µÈ´ıIOÊäÈë£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_wait_input", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_wait_input(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, double TimeOut, Int32 mark);
        //Ïà¶ÔÓÚ¹ì¼£ÆğµãIOÖÍºóÊä³ö£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_delay_outbit_to_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_delay_outbit_to_start(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, double delay_value, UInt16 delay_mode, double ReverseTime);      
        //Ïà¶ÔÓÚ¹ì¼£ÖÕµãIOÖÍºóÊä³ö£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_delay_outbit_to_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_delay_outbit_to_stop(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, double delay_time, double ReverseTime);      
        //Ïà¶ÔÓÚ¹ì¼£ÖÕµãIOÌáÇ°Êä³ö£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_ahead_outbit_to_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_ahead_outbit_to_stop(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, double ahead_value, UInt16 ahead_mode, double ReverseTime);  
        //Á¬Ğø²å²¹¾«È·Î»ÖÃCMPÊä³ö£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_accurate_outbit_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_accurate_outbit_unit(UInt16 CardNo, UInt16 Crd, UInt16 cmp_no, UInt16 on_off, UInt16 map_axis, double abs_pos, UInt16 pos_source, double ReverseTime);    
        //Á¬Ğø²å²¹Á¢¼´IOÊä³ö£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_write_outbit(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, double ReverseTime);     
        //Çå³ı¶ÎÄÚÎ´Ö´ĞĞÍêµÄIO£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_clear_io_action", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_clear_io_action(UInt16 CardNo, UInt16 Crd, UInt32 IoMask);    
        //Á¬Ğø²å²¹ÔİÍ£¼°Òì³£Ê±IOÊä³ö×´Ì¬£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_set_pause_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_set_pause_output(UInt16 CardNo, UInt16 Crd, UInt16 action, Int32 mask, Int32 state);     //ÔİÍ£Ê±IOÊä³ö action 0, ²»¹¤×÷£»1£¬ ÔİÍ£Ê±Êä³öio_state; 2 ÔİÍ£Ê±Êä³öio_state,¼ÌĞøÔËĞĞÊ±Ê×ÏÈ»Ö¸´Ô­À´µÄio; 3,ÔÚ2µÄ»ù´¡ÉÏ£¬Í£Ö¹Ê±Ò²ÉúĞ§¡£
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_get_pause_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_get_pause_output(UInt16 CardNo, UInt16 Crd, ref UInt16 action, ref Int32 mask, ref Int32 state);
        //ÑÓÊ±Ö¸Áî£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_delay(UInt16 CardNo, UInt16 Crd, double delay_time, Int32 mark);     //Ìí¼ÓÑÓÊ±Ö¸Áî
        //IOÊä³öÑÓÊ±·­×ª£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_reverse_outbit(UInt16 CardNo, UInt16 Crd, UInt16 bitno, double reverse_time);
        //IOÑÓÊ±Êä³ö£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_delay_outbit(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, double delay_time);
        //Á¬Ğø²å²¹µ¥ÖáÔË¶¯£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_pmove_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_pmove_unit(UInt16 CardNo, UInt16 Crd, UInt16 Axis, double dist, UInt16 posi_mode, UInt16 mode, Int32 mark); //Á¬Ğø²å²¹ÖĞ¿ØÖÆÖ¸¶¨ÍâÖáÔË¶¯
        //Á¬Ğø²å²¹Ö±Ïß²å²¹£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_line_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_line_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, UInt16 posi_mode, Int32 mark); //Á¬Ğø²å²¹Ö±Ïß
        //Á¬Ğø²å²¹Ô²ĞÄÔ²»¡²å²¹£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_arc_move_center_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_arc_move_center_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double[] Cen_Pos, UInt16 Arc_Dir, Int32 Circle, UInt16 posi_mode, Int32 
mark);    
        //Á¬Ğø²å²¹°ë¾¶Ô²»¡²å²¹£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_arc_move_radius_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_arc_move_radius_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double Arc_Radius, UInt16 Arc_Dir, Int32 Circle, UInt16 posi_mode, Int32 
mark);   
        //Á¬Ğø²å²¹3µãÔ²»¡²å²¹£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_arc_move_3points_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_arc_move_3points_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double[] Mid_Pos, Int32 Circle, UInt16 posi_mode, Int32 mark);     
        //Á¬Ğø²å²¹¾ØĞÎ²å²¹£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_rectangle_move_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_rectangle_move_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] TargetPos, double[] MaskPos, Int32 Count, UInt16 rect_mode, UInt16 posi_mode, Int32 mark
);     
        //ÉèÖÃÂİĞıÏß²å²¹ÔË¶¯Ä£Ê½£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_set_involute_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_set_involute_mode(UInt16 CardNo, UInt16 Crd, UInt16 mode);      //ÉèÖÃÂİĞıÏßÊÇ·ñ·â±Õ
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_get_involute_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_get_involute_mode(UInt16 CardNo, UInt16 Crd, ref UInt16 mode);   //¶ÁÈ¡ÂİĞıÏßÊÇ·ñ·â±ÕÉèÖÃ
        //£¨±¸ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_line_unit_extern(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double[] Cen_Pos, UInt16 posi_mode, Int32 mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_arc_move_center_unit_extern(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double[] Cen_Pos, double Arc_Radius, UInt16 posi_mode, Int32 mark);
        //ÉèÖÃ¶ÁÈ¡ÁúÃÅ¸úËæÄ£Ê½£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_gear_follow_profile(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 master_axis, double ratio);//Ë«ZÖá
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_gear_follow_profile(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 master_axis, ref double ratio);
              
        //--------------------PWM¿ØÖÆ----------------------
        //PWM¿ØÖÆ£¨±¸ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_pwm_pin(UInt16 CardNo, UInt16 portno, UInt16 ON_OFF, double dfreqency, double dduty);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pwm_pin(UInt16 CardNo, UInt16 portno, ref UInt16 ON_OFF, ref double dfreqency, ref double dduty);
        //ÉèÖÃ¶ÁÈ¡PWMÊ¹ÄÜ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_pwm_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_pwm_enable(UInt16 CardNo, UInt16 enable);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_pwm_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_pwm_enable(UInt16 CardNo, ref UInt16 enable);
        //ÉèÖÃ¶ÁÈ¡PWMÁ¢¼´Êä³ö£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_pwm_output(UInt16 CardNo, UInt16 pwm_no, double fDuty, double fFre);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_pwm_output(UInt16 CardNo, UInt16 pwm_no, ref double fDuty, ref double fFre);        
        //Á¬Ğø²å²¹PWMÊä³ö£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_set_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_set_pwm_output(UInt16 CardNo, UInt16 Crd, UInt16 pwm_no, double fDuty, double fFre);
        //¸ßËÙPWM¹¦ÄÜ£¨±¸ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_pwm_enable_extern(UInt16 CardNo, UInt16 channel, UInt16 enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pwm_enable_extern(UInt16 CardNo, UInt16 channel, ref UInt16 enable);
        //ÉèÖÃPWM¿ª¹Ø¶ÔÓ¦µÄÕ¼¿Õ±È£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_pwm_onoff_duty", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_pwm_onoff_duty(UInt16 CardNo, UInt16 PwmNo, double fOnDuty, double fOffDuty);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_pwm_onoff_duty", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_pwm_onoff_duty(UInt16 CardNo, UInt16 PwmNo, ref double fOnDuty, ref double fOffDuty);
        //Á¬Ğø²å²¹PWMËÙ¶È¸úËæ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_set_pwm_follow_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_set_pwm_follow_speed(UInt16 CardNo, UInt16 Crd, UInt16 pwm_no, UInt16 mode, double MaxVel, double MaxValue, double OutValue);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_get_pwm_follow_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_get_pwm_follow_speed(UInt16 CardNo, UInt16 Crd, UInt16 pwm_no, ref UInt16 mode, ref double MaxVel, ref double MaxValue, ref double OutValue);
        //Á¬Ğø²å²¹Ïà¶Ô¹ì¼£ÆğµãPWMÖÍºóÊä³ö£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_delay_pwm_to_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_delay_pwm_to_start(UInt16 CardNo, UInt16 Crd, UInt16 pwmno, UInt16 on_off, double delay_value, UInt16 delay_mode, double ReverseTime);
        //Á¬Ğø²å²¹Ïà¶Ô¹ì¼£ÖÕµãPWMÌáÇ°Êä³ö£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_ahead_pwm_to_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_ahead_pwm_to_stop(UInt16 CardNo, UInt16 Crd, UInt16 pwmno, UInt16 on_off, double ahead_value, UInt16 ahead_mode, double ReverseTime);
        //Á¬Ğø²å²¹PWMÁ¢¼´Êä³ö£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_write_pwm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_write_pwm(UInt16 CardNo, UInt16 Crd, UInt16 pwmno, UInt16 on_off, double ReverseTime);

        //--------------------ADDAÊä³ö----------------------
        //¿ØÖÆ¿¨½ÓÏßºĞDAÊä³ö£¬ÉèÖÃDAÊä³öÊ¹ÄÜ£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_da_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_da_enable(UInt16 CardNo, UInt16 enable);      
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_da_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_da_enable(UInt16 CardNo, ref UInt16 enable);
        //ÉèÖÃDAÊä³ö£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_da_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_da_output(UInt16 CardNo, UInt16 channel, double Vout);   
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_da_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_da_output(UInt16 CardNo, UInt16 channel, ref double Vout);
        //¿ØÖÆ¿¨½ÓÏßºĞADÊäÈë£¬¶ÁÈ¡ADÊäÈë£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_ad_input(ushort CardNo, ushort channel, ref double Vout);
        //ÉèÖÃÁ¬ĞøDAÊ¹ÄÜ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_da_output(UInt16 CardNo, UInt16 Crd, UInt16 channel, double Vout);
        //ÉèÖÃÁ¬ĞøDAÊ¹ÄÜ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_set_da_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_set_da_enable(ushort CardNo, ushort Crd, ushort enable, ushort channel, int mark);
        //±àÂëÆ÷da¸úËæ£¨Ô¤Áô£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_encoder_da_follow_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_encoder_da_follow_enable(ushort CardNo, ushort axis, ushort enable);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_encoder_da_follow_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_encoder_da_follow_enable(ushort CardNo, ushort axis, ref ushort enable);
        //Á¬Ğø²å²¹DAËÙ¶È¸úËæ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_set_da_follow_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_set_da_follow_speed(ushort CardNo, ushort Crd, ushort da_no, double MaxVel, double MaxValue, double acc_offset, double dec_offset, double acc_dist, double dec_dist);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_conti_get_da_follow_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_conti_get_da_follow_speed(ushort CardNo, ushort Crd, ushort da_no, ref double MaxVel, ref double MaxValue, ref double acc_offset, ref double dec_offset, ref double acc_dist, ref double 
dec_dist);

        //Ğ¡Ô²ÏŞËÙÊ¹ÄÜ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_arc_limit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_arc_limit(UInt16 CardNo, UInt16 Crd, UInt16 Enable, double MaxCenAcc, double MaxArcError);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_arc_limit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_arc_limit(UInt16 CardNo, UInt16 Crd, ref UInt16 Enable, ref double MaxCenAcc, ref double MaxArcError);
        //£¨Ô¤Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_IoFilter(UInt16 CardNo, UInt16 bitno, double filter);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_IoFilter(UInt16 CardNo, UInt16 bitno, ref double filter);
        //Âİ¾à²¹³¥£¨¾ÉÖ¸Áî£¬²»Ê¹ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_lsc_index_value(UInt16 CardNo, UInt16 axis, UInt16 IndexID, Int32 IndexValue);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_lsc_index_value(UInt16 CardNo, UInt16 axis, UInt16 IndexID, ref Int32 IndexValue);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_lsc_config(UInt16 CardNo, UInt16 axis, UInt16 Origin, UInt32 Interal, UInt32 NegIndex, UInt32 PosIndex, double Ratio);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_lsc_config(UInt16 CardNo, UInt16 axis, ref UInt16 Origin, ref UInt32 Interal, ref UInt32 NegIndex, ref UInt32 PosIndex, ref double Ratio);
        //¿´ÃÅ¹·¾ÉÖ¸Áî£¬²»Ê¹ÓÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_watchdog(UInt16 CardNo, UInt16 enable, UInt32 time);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_call_watchdog(UInt16 CardNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_diagnoseData(UInt16 CardNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_cmd_end(UInt16 CardNo, UInt16 Crd, UInt16 enable);
        //ÇøÓòÈíÏŞÎ»£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_zone_limit_config(UInt16 CardNo, UInt16[] axis, UInt16[] Source, Int32 x_pos_p, Int32 x_pos_n, Int32 y_pos_p, Int32 y_pos_n, UInt16 action_para);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_zone_limit_config(UInt16 CardNo, UInt16[] axis, UInt16[] Source, ref Int32 x_pos_p, ref Int32 x_pos_n, ref Int32 y_pos_p, ref Int32 y_pos_n, ref UInt16 action_para);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_zone_limit_enable(UInt16 CardNo, UInt16 enable);
        //Öá»¥Ëø¹¦ÄÜ£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_interlock_config(UInt16 CardNo, UInt16[] axis, UInt16[] Source, Int32 delta_pos, UInt16 action_para);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_interlock_config(UInt16 CardNo, UInt16[] axis, UInt16[] Source, ref Int32 delta_pos, ref UInt16 action_para);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_interlock_enable(UInt16 CardNo, UInt16 enable);
        //ÁúÃÅÄ£Ê½µÄÎó²î±£»¤£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_grant_error_protect(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt32 dstp_error, UInt32 emg_error);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_grant_error_protect(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt32 dstp_error, ref UInt32 emg_error);
        //ÁúÃÅÄ£Ê½µÄÎó²î±£»¤µ±Á¿º¯Êı£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_grant_error_protect_unit(UInt16 CardNo, UInt16 axis, UInt16 enable, double dstp_error, double emg_error);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_grant_error_protect_unit(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref double dstp_error, ref double emg_error);

        //Îï¼ş·Ö¼ğ¹¦ÄÜ £¨·Ö¼ğ¹Ì¼ş×¨ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_camerablow_config(UInt16 CardNo, UInt16 camerablow_en, Int32 cameraPos, UInt16 piece_num, Int32 piece_distance, UInt16 axis_sel, Int32 latch_distance_min);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_camerablow_config(UInt16 CardNo, ref UInt16 camerablow_en, ref Int32 cameraPos, ref UInt16 piece_num, ref Int32 piece_distance, ref UInt16 axis_sel, ref Int32 latch_distance_min);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_clear_camerablow_errorcode(UInt16 CardNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_camerablow_errorcode(UInt16 CardNo, ref UInt16 errorcode);
        //ÅäÖÃÍ¨ÓÃÊäÈë£¨0~15£©×öÎªÖáµÄÏŞÎ»ĞÅºÅ£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_io_limit_config(UInt16 CardNo, UInt16 portno, UInt16 enable, UInt16 axis_sel, UInt16 el_mode, UInt16 el_logic);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_io_limit_config(UInt16 CardNo, UInt16 portno, ref UInt16 enable, ref UInt16 axis_sel, ref UInt16 el_mode, ref UInt16 el_logic);
        //ÊÖÂÖÂË²¨²ÎÊı£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_handwheel_filter(UInt16 CardNo, UInt16 axis, double filter_factor);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_handwheel_filter(UInt16 CardNo, UInt16 axis, ref double filter_factor);
        //¶ÁÈ¡×ø±êÏµ¸÷ÖáµÄµ±Ç°¹æ»®×ø±ê£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_get_interp_map(UInt16 CardNo, UInt16 Crd, ref UInt16 AxisNum, UInt16[] AxisList, double[] pPosList);
        //×ø±êÏµ´íÎó´úÂë £¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_get_crd_errcode(UInt16 CardNo, UInt16 Crd, ref UInt16 errcode);
        //±£Áô
        [DllImport("LTDMC.dll")]
        public static extern short dmc_line_unit_follow(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Dist, UInt16 posi_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_line_unit_follow(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] pPosList, UInt16 posi_mode, Int32 mark);
        //Á¬Ğø²å²¹»º³åÇøDA²Ù×÷£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_da_action(UInt16 CardNo, UInt16 Crd, UInt16 mode, UInt16 portno, double dvalue);
        //¶Á±àÂëÆ÷ËÙ¶È£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_encoder_speed(UInt16 CardNo, UInt16 Axis, ref double current_speed);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_axis_follow_line_enable(UInt16 CardNo, UInt16 Crd, UInt16 enable_flag);
        //²å²¹ÖáÂö³å²¹³¥£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_interp_compensation(UInt16 CardNo, UInt16 axis, double dvalue, double time);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_interp_compensation(UInt16 CardNo, UInt16 axis, ref double dvalue, ref double time);
        //¶ÁÈ¡Ïà¶ÔÓÚÆğµãµÄ¾àÀë£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_distance_to_start(UInt16 CardNo, UInt16 Crd, ref double distance_x, ref double distance_y, Int32 imark);
        //ÉèÖÃ±êÖ¾Î» ±íÊ¾ÊÇ·ñ¿ªÊ¼¼ÆËãÏà¶ÔÆğµã£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_start_distance_flag(UInt16 CardNo, UInt16 Crd, UInt16 flag);

        //µ¶Ïò¸úËæ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_gear_unit(UInt16 CardNo, UInt16 Crd, UInt16 axis, double dist, UInt16 follow_mode, Int32 imark);
        //¹ì¼£ÄâºÏÊ¹ÄÜÉèÖÃ£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_path_fitting_enable(UInt16 CardNo, UInt16 Crd, UInt16 enable);
        //--------------------Âİ¾à²¹³¥----------------------
        //Âİ¾à²¹³¥¹¦ÄÜ(ĞÂ)£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_enable_leadscrew_comp(UInt16 CardNo, UInt16 axis, UInt16 enable);
        //ÅäÖÃÂß¼­²¹³¥²ÎÊı£¨Âö³å£©£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_leadscrew_comp_config(UInt16 CardNo, UInt16 axis, UInt16 n, Int32 startpos, Int32 lenpos, Int32[] pCompPos, Int32[] pCompNeg);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_leadscrew_comp_config(UInt16 CardNo, UInt16 axis, ref UInt16 n, ref int startpos, ref int lenpos, int[] pCompPos, int[] pCompNeg);
        //ÅäÖÃÂß¼­²¹³¥²ÎÊı£¨µ±Á¿£©£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_leadscrew_comp_config_unit(UInt16 CardNo, UInt16 axis, UInt16 n, double startpos, double lenpos, double[] pCompPos, double[] pCompNeg);       
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_leadscrew_comp_config_unit(UInt16 CardNo, UInt16 axis, ref UInt16 n, ref double startpos, ref double lenpos, double[] pCompPos, double[] pCompNeg);
        //Âİ¾à²¹³¥Ç°µÄÂö³åÎ»ÖÃ£¬±àÂëÆ÷Î»ÖÃ//20191025£¨ÊÊÓÃÓÚDMC3000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_position_ex(UInt16 CardNo, UInt16 axis, ref double pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_encoder_ex(UInt16 CardNo, UInt16 axis, ref double pos);
        //Âİ¾à²¹³¥Ç°µÄÂö³åÎ»ÖÃ£¬±àÂëÆ÷Î»ÖÃ µ±Á¿£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_position_ex_unit(UInt16 CardNo, UInt16 axis, ref double pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_encoder_ex_unit(UInt16 CardNo, UInt16 axis, ref double pos);

        //Ö¸¶¨Öá×ö¶¨³¤Î»ÒÆÔË¶¯ °´¹Ì¶¨ÇúÏßÔË¶¯£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_t_pmove_extern(UInt16 CardNo, UInt16 axis, double MidPos, double TargetPos, double Min_Vel, double Max_Vel, double stop_Vel, double acc, double dec, UInt16 posi_mode);
        //
        [DllImport("LTDMC.dll")]
        public static extern short dmc_t_pmove_extern_unit(UInt16 CardNo, UInt16 axis, double MidPos, double TargetPos, double Min_Vel, double Max_Vel, double stop_Vel, double acc, double dec, UInt16 posi_mode);
        //ÉèÖÃÂö³å¼ÆÊıÖµºÍ±àÂëÆ÷·´À¡ÖµÖ®¼ä²îÖµµÄ±¨¾¯·§Öµ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_pulse_encoder_count_error(UInt16 CardNo, UInt16 axis, UInt16 error);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pulse_encoder_count_error(UInt16 CardNo, UInt16 axis, ref UInt16 error);
        //¼ì²éÂö³å¼ÆÊıÖµºÍ±àÂëÆ÷·´À¡ÖµÖ®¼ä²îÖµÊÇ·ñ³¬¹ı±¨¾¯·§Öµ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_check_pulse_encoder_count_error(UInt16 CardNo, UInt16 axis, ref Int32 pulse_position, ref Int32 enc_position);
        //ÉèÖÃ/»Ø¶ÁÂö³å¼ÆÊıÖµºÍ±àÂëÆ÷·´À¡ÖµÖ®¼ä²îÖµµÄ±¨¾¯ãĞÖµunit£¨ÊÊÓÃÓÚDMC5X10Âö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_pulse_encoder_count_error_unit(ushort CardNo, ushort axis, double error);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pulse_encoder_count_error_unit(ushort CardNo, ushort axis, ref double error);
        //¼ì²éÂö³å¼ÆÊıÖµºÍ±àÂëÆ÷·´À¡ÖµÖ®¼ä²îÖµÊÇ·ñ³¬¹ı±¨¾¯ãĞÖµunit£¨ÊÊÓÃÓÚDMC5X10Âö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_check_pulse_encoder_count_error_unit(ushort CardNo, ushort axis, ref double pulse_position, ref double enc_position);
        //Ê¹ÄÜºÍÉèÖÃ¸ú×Ù±àÂëÆ÷Îó²î²»ÔÚ·¶Î§ÄÚÊ±ÖáµÄÍ£Ö¹Ä£Ê½£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_encoder_count_error_action_config(UInt16 CardNo, UInt16 enable, UInt16 stopmode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_encoder_count_error_action_config(UInt16 CardNo, ref UInt16 enable, ref UInt16 stopmode);
        
        //ĞÂÎï¼ş·Ö¼ğ¹¦ÄÜ ·Ö¼ğ¹Ì¼ş×¨ÓÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_close(UInt16 CardNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_start(UInt16 CardNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_init_config(UInt16 CardNo, UInt16 cameraCount, Int32[] pCameraPos, UInt16[] pCamIONo, UInt32 cameraTime, UInt16 cameraTrigLevel, UInt16 blowCount, Int32[] pBlowPos, UInt16[]
 pBlowIONo, UInt32 blowTime, UInt16 blowTrigLevel, UInt16 axis, UInt16 dir, UInt16 checkMode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_camera_trig_count(UInt16 CardNo, UInt16 cameraNum, UInt32 cameraTrigCnt);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_get_camera_trig_count(UInt16 CardNo, UInt16 cameraNum, ref UInt32 pCameraTrigCnt, UInt16 count);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_blow_trig_count(UInt16 CardNo, UInt16 blowNum, UInt32 blowTrigCnt);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_get_blow_trig_count(UInt16 CardNo, UInt16 blowNum, ref UInt32 pBlowTrigCnt, UInt16 count);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_get_camera_config(UInt16 CardNo, UInt16 index, ref Int32 pos, ref UInt32 trigTime, ref UInt16 ioNo, ref UInt16 trigLevel);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_get_blow_config(UInt16 CardNo, UInt16 index, ref Int32 pos, ref UInt32 trigTime, ref UInt16 ioNo, ref UInt16 trigLevel);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_get_blow_status(UInt16 CardNo, ref Int32 trigCntAll, ref UInt16 trigMore, ref UInt16 trigLess);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_trig_blow(UInt16 CardNo, UInt16 blowNum);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_blow_enable(UInt16 CardNo, UInt16 blowNum, UInt16 enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_piece_config(UInt16 CardNo, UInt32 maxWidth, UInt32 minWidth, UInt32 minDistance, UInt32 minTimeIntervel);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_get_piece_status(UInt16 CardNo, ref UInt32 pieceFind, ref UInt32 piecePassCam, ref UInt32 dist2next, ref UInt32 pieceWidth);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_cam_trig_phase(UInt16 CardNo, UInt16 blowNo, double coef);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_blow_trig_phase(UInt16 CardNo, UInt16 blowNo, double coef);
        
        //ÄÚ²¿Ê¹ÓÃ£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_sevon_enable(UInt16 CardNo, UInt16 axis, UInt16 on_off);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_sevon_enable(UInt16 CardNo, UInt16 axis);

        //Á¬Ğø±àÂëÆ÷da¸úËæ£¨ÊÊÓÃÓÚDMC5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_encoder_da_follow_enable(UInt16 CardNo, UInt16 Crd, UInt16 axis, UInt16 enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_get_encoder_da_follow_enable(UInt16 CardNo, UInt16 Crd, ref UInt16 axis, ref UInt16 enable);
        //ÉèÖÃÎ»ÖÃÎó²î´ø£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_set_factor_error", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_factor_error(UInt16 CardNo, UInt16 axis, double factor, Int32 error);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_get_factor_error", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_factor_error(UInt16 CardNo, UInt16 axis, ref double factor, ref Int32 error);
        //ÉèÖÃ/»Ø¶ÁÎ»ÖÃÎó²î´øunit£¨ÊÊÓÃÓÚDMC5X10Âö³å¿¨¡¢EtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_factor_error_unit(ushort CardNo, ushort axis, double factor, double error);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_factor_error_unit(ushort CardNo, ushort axis, ref double factor, ref double error);
        //±£Áô
        [DllImport("LTDMC.dll")]
        public static extern short dmc_check_done_pos(UInt16 CardNo, UInt16 axis, UInt16 posi_mode);
        //±£Áô
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_factor(UInt16 CardNo, UInt16 axis, double factor);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_error(UInt16 CardNo, UInt16 axis, Int32 error);
        //¼ì²âÖ¸Áîµ½Î»£¨ÊÊÓÃÓÚËùÓĞÂö³å¿¨¡¢×ÜÏß¿¨£©
        [DllImport("LTDMC.dll", EntryPoint = "dmc_check_success_pulse", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_check_success_pulse(UInt16 CardNo, UInt16 axis);
        [DllImport("LTDMC.dll", EntryPoint = "dmc_check_success_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_check_success_encoder(UInt16 CardNo, UInt16 axis);

        //IO¼°±àÂëÆ÷¼ÆÊı¹¦ÄÜ£¨±£Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_io_count_profile(UInt16 CardNo, UInt16 chan, UInt16 bitno,UInt16 mode,double filter, double count_value, UInt16[] axis_list, UInt16 axis_num, UInt16 stop_mode );
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_io_count_profile(UInt16 CardNo, UInt16 chan, ref UInt16 bitno,ref UInt16 mode,ref double filter, ref double count_value, UInt16[] axis_list, ref UInt16 axis_num, ref UInt16 
stop_mode );
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_io_count_enable(UInt16 CardNo, UInt16 chan, UInt16 ifenable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_clear_io_count(UInt16 CardNo, UInt16 chan);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_io_count_value_extern(UInt16 CardNo, UInt16 chan, ref Int32 current_value);
        //±£Áô
        [DllImport("LTDMC.dll")]
        public static extern short dmc_change_speed_extend(UInt16 CardNo,UInt16 axis,double Curr_Vel, double Taccdec, UInt16 pin_num, UInt16 trig_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_follow_vector_speed_move(UInt16 CardNo,UInt16 axis,UInt16 Follow_AxisNum,UInt16[] Follow_AxisList,double ratio);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_line_unit_extend(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] pPosList, UInt16 posi_mode, double Extend_Len, UInt16 enable,Int32 mark); //Á¬Ğø²å²¹Ö±Ïß
     
        //×ÜÏß²ÎÊı
        [DllImport("LTDMC.dll", EntryPoint = "nmc_download_configfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_download_configfile(UInt16 CardNo, UInt16 PortNum, String FileName);//×ÜÏßENIÅäÖÃÎÄ¼ş
        [DllImport("LTDMC.dll", EntryPoint = "nmc_download_mapfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_download_mapfile(UInt16 CardNo, String FileName);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_upload_configfile(UInt16 CardNo, UInt16 PortNum, String FileName);
        [DllImport("LTDMC.dll", EntryPoint = "nmc_set_manager_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_set_manager_para(UInt16 CardNo, UInt16 PortNum, Int32 baudrate, UInt16 ManagerID);
        [DllImport("LTDMC.dll", EntryPoint = "nmc_get_manager_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_manager_para(UInt16 CardNo, UInt16 PortNum, ref UInt32 baudrate, ref UInt16 ManagerID);
        [DllImport("LTDMC.dll", EntryPoint = "nmc_set_manager_od", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_set_manager_od(UInt16 CardNo, UInt16 PortNum, UInt16 index, UInt16 subindex, UInt16 valuelength, UInt32 value);
        [DllImport("LTDMC.dll", EntryPoint = "nmc_get_manager_od", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_manager_od(UInt16 CardNo, UInt16 PortNum, UInt16 index, UInt16 subindex, UInt16 valuelength, ref UInt32 value);

        [DllImport("LTDMC.dll", EntryPoint = "nmc_get_total_axes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_total_axes(ushort CardNo, ref uint TotalAxis);
        [DllImport("LTDMC.dll", EntryPoint = "nmc_get_total_ionum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_total_ionum(UInt16 CardNo, ref UInt16 TotalIn, ref UInt16 TotalOut);
        [DllImport("LTDMC.dll", EntryPoint = "nmc_get_LostHeartbeat_Nodes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_LostHeartbeat_Nodes(UInt16 CardNo, UInt16 PortNum, UInt16[] NodeID, ref UInt16 NodeNum);
        [DllImport("LTDMC.dll", EntryPoint = "nmc_get_EmergeneyMessege_Nodes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_EmergeneyMessege_Nodes(UInt16 CardNo, UInt16 PortNum, UInt32[] NodeMsg, ref UInt16 MsgNum);
        [DllImport("LTDMC.dll", EntryPoint = "nmc_SendNmtCommand", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_SendNmtCommand(UInt16 CardNo, UInt16 PortNum, UInt16 NodeID, UInt16 NmtCommand);
        [DllImport("LTDMC.dll", EntryPoint = "nmc_syn_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_syn_move(UInt16 CardNo, UInt16 AxisNum, UInt16[] AxisList, Int32[] Position, UInt16[] PosiMode);
        //
        [DllImport("LTDMC.dll")]
        public static extern short nmc_syn_move_unit(UInt16 CardNo, UInt16 AxisNum, UInt16[] AxisList, double[] Position, UInt16[] PosiMode);
        //×ÜÏß¶àÖáÍ¬²½ÔË¶¯
        [DllImport("LTDMC.dll")]
        public static extern short nmc_sync_pmove_unit(UInt16 CardNo, UInt16 AxisNum, UInt16[] AxisList, double[] Dist, UInt16[] PosiMode);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_sync_vmove_unit(UInt16 CardNo, UInt16 AxisNum, UInt16[] AxisList, UInt16[] Dir);
        //ÉèÖÃÖ÷Õ¾²ÎÊı
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_master_para(UInt16 CardNo, UInt16 PortNum, UInt16 Baudrate, UInt32 NodeCnt, UInt16 MasterId);
        //¶ÁÈ¡Ö÷Õ¾²ÎÊı
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_master_para(UInt16 CardNo, UInt16 PortNum, ref UInt16 Baudrate, ref UInt32 NodeCnt, ref UInt16 MasterId);
        //»ñÈ¡×ÜÏßADDAÊäÈëÊä³ö¿ÚÊı
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_total_adcnum(ushort CardNo, ref ushort TotalIn, ref ushort TotalOut);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_controller_workmode(ushort CardNo, ushort controller_mode);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_controller_workmode(ushort CardNo, ref ushort controller_mode);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_cycletime(ushort CardNo, ushort FieldbusType, int CycleTime);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_cycletime(ushort CardNo, ushort FieldbusType, ref int CycleTime);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_node_od(ushort CardNo, ushort PortNum, ushort nodenum, ushort index, ushort subindex, ushort valuelength, ref int value);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_node_od(ushort CardNo, ushort PortNum, ushort nodenum, ushort index, ushort subindex, ushort valuelength, int value);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_reset_to_factory(ushort CardNo, ushort PortNum, ushort NodeNum);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_alarm_clear(ushort CardNo, ushort PortNum, ushort nodenum);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_slave_nodes(ushort CardNo, ushort PortNum, ushort BaudRate, ref ushort NodeId, ref ushort NodeNum);
        
        //Öá×´Ì¬»ú
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_axis_state_machine(ushort CardNo, ushort axis, ref ushort Axis_StateMachine);
        //»ñÈ¡Öá×´Ì¬×Ö
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_axis_statusword(ushort CardNo, ushort axis, ref int statusword);
        //»ñÈ¡ÖáÅäÖÃ¿ØÖÆÄ£Ê½£¬·µ»ØÖµ£¨6»ØÁãÄ£Ê½£¬8cspÄ£Ê½£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_axis_setting_contrlmode(ushort CardNo, ushort axis, ref int contrlmode);
        //ÉèÖÃ×ÜÏßÖá¿ØÖÆ×Ö
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_axis_contrlword(ushort CardNo, ushort axis, int contrlword);
        //»ñÈ¡×ÜÏßÖá¿ØÖÆ×Ö
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_axis_contrlword(ushort CardNo, ushort axis, ref int contrlword);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_axis_type(ushort CardNo, ushort axis, ref ushort Axis_Type);
        //»ñÈ¡×ÜÏßÊ±¼äÁ¿£¬Æ½¾ùÊ±¼ä£¬×î´óÊ±¼ä£¬Ö´ĞĞÖÜÆÚÊı
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_consume_time_fieldbus(ushort CardNo, ushort Fieldbustype, ref uint Average_time, ref uint Max_time, ref UInt64 Cycles);
        //Çå³ıÊ±¼äÁ¿
        [DllImport("LTDMC.dll")]
        public static extern short nmc_clear_consume_time_fieldbus(ushort CardNo, ushort Fieldbustype);
        //×ÜÏßµ¥ÖáÊ¹ÄÜº¯Êı 255±íÊ¾È«Ê¹ÄÜ
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_axis_enable(ushort CardNo, ushort axis);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_axis_disable(ushort CardNo, ushort axis);
        // »ñÈ¡ÖáµÄ´ÓÕ¾ĞÅÏ¢
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_axis_node_address(ushort CardNo, ushort axis, ref ushort SlaveAddr, ref ushort Sub_SlaveAddr);
        //»ñÈ¡×ÜÏßÖáÊı
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_total_slaves(ushort CardNo, ushort PortNum, ref ushort TotalSlaves);
        [DllImport("LTDMC.dll")]
        //×ÜÏß»ØÔ­µãº¯Êı
        public static extern short nmc_set_home_profile(ushort CardNo, ushort axis, ushort home_mode, double Low_Vel, double High_Vel, double Tacc, double Tdec, double offsetpos);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_home_profile(ushort CardNo, ushort axis, ref ushort home_mode, ref double Low_Vel, ref double High_Vel, ref double Tacc, ref double Tdec, ref double offsetpos);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_home_move(ushort CardNo, ushort axis);
        //
        [DllImport("LTDMC.dll")]
        public static extern short nmc_start_scan_ethercat(ushort CardNo, ushort AddressID);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_stop_scan_ethercat(ushort CardNo, ushort AddressID);
        //ÉèÖÃÖáµÄÔËĞĞÄ£Ê½ 1ÎªppÄ£Ê½£¬6Îª»ØÁãÄ£Ê½£¬8ÎªcspÄ£Ê½
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_axis_run_mode(ushort CardNo, ushort axis, ushort run_mode);
        //Çå³ı¶Ë¿Ú±¨¾¯
        [DllImport("LTDMC.dll")]
        public static extern short nmc_clear_alarm_fieldbus(ushort CardNo, ushort PortNum);
        //Í£Ö¹ethercat×ÜÏß,·µ»Ø0±íÊ¾³É¹¦£¬ÆäËû²ÎÊı±íÊ¾²»³É¹¦
        [DllImport("LTDMC.dll")]
        public static extern short nmc_stop_etc(ushort CardNo, ref ushort ETCState);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_axis_contrlmode(ushort CardNo, ushort Axis, ref int Contrlmode);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_axis_io_in(ushort CardNo, ushort axis);

        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_axis_io_out(UInt16 CardNo, UInt16 axis, UInt32 iostate);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_axis_io_out(UInt16 CardNo, UInt16 axis);
        // »ñÈ¡×ÜÏß¶Ë¿Ú´íÎóÂë
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_errcode(ushort CardNo, ushort channel, ref ushort errcode);
        // Çå³ı×ÜÏß¶Ë¿Ú´íÎóÂë
        [DllImport("LTDMC.dll")]
        public static extern short nmc_clear_errcode(ushort CardNo, ushort channel);
        // »ñÈ¡×ÜÏßÖá´íÎóÂë
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_axis_errcode(ushort CardNo, ushort axis, ref ushort Errcode);
        // Çå³ı×ÜÏßÖá´íÎóÂë
        [DllImport("LTDMC.dll")]
        public static extern short nmc_clear_axis_errcode(ushort CardNo, ushort axis);

        //RTEX¿¨Ìí¼Óº¯Êı
        [DllImport("LTDMC.dll")]
        public static extern short nmc_start_connect(UInt16 CardNo, UInt16 chan, ref UInt16 info, ref UInt16 len);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_vendor_info(UInt16 CardNo, UInt16 axis, Byte[] info, ref UInt16 len);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_slave_type_info(UInt16 CardNo, UInt16 axis, Byte[] info, ref UInt16 len);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_slave_name_info(UInt16 CardNo, UInt16 axis, Byte[] info, ref UInt16 len);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_slave_version_info(UInt16 CardNo, UInt16 axis, Byte[] info, ref UInt16 len);

        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_parameter(UInt16 CardNo, UInt16 axis, UInt16 index, UInt16 subindex, UInt32 para_data);
        /**************************************************************
        *¹¦ÄÜËµÃ÷£ºRTEXÇı¶¯Æ÷Ğ´EEPROM²Ù×÷
        **************************************************************/
        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_slave_eeprom(UInt16 CardNo, UInt16 axis);

        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_parameter(UInt16 CardNo, UInt16 axis, UInt16 index, UInt16 subindex, ref UInt32 para_data);
        /**************************************************************
         * *index:rtexÇı¶¯Æ÷µÄ²ÎÊı·ÖÀà
         * *subindex:rtexÇı¶¯Æ÷ÔÚindexÀà±ğÏÂµÄ²ÎÊı±àºÅ
         * *para_data:¶Á³öµÄ²ÎÊıÖµ
         * **************************************************************/
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_parameter_attributes(UInt16 CardNo, UInt16 axis, UInt16 index, UInt16 subindex, ref UInt32 para_data);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_cmdcycletime(UInt16 CardNo, UInt16 PortNum, UInt32 cmdtime);
        //ÉèÖÃRTEX×ÜÏßÖÜÆÚ±È(us)
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_cmdcycletime(UInt16 CardNo, UInt16 PortNum, ref UInt32 cmdtime);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_config_atuo_log(UInt16 CardNo, UInt16 ifenable, UInt16 dir, UInt16 byte_index, UInt16 mask, UInt16 condition, UInt32 counter);

        //À©Õ¹PDO
        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_rxpdo_extra(UInt16 CardNo, UInt16 PortNum, UInt16 address, UInt16 DataLen, Int32 Value);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_rxpdo_extra(UInt16 CardNo, UInt16 PortNum, UInt16 address, UInt16 DataLen, ref Int32 Value);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_txpdo_extra(UInt16 CardNo, UInt16 PortNum, UInt16 address, UInt16 DataLen, ref Int32 Value);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_rxpdo_extra_uint(UInt16 CardNo, UInt16 PortNum, UInt16 address, UInt16 DataLen, UInt32 Value);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_rxpdo_extra_uint(UInt16 CardNo, UInt16 PortNum, UInt16 address, UInt16 DataLen, ref UInt32 Value);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_txpdo_extra_uint(UInt16 CardNo, UInt16 PortNum, UInt16 address, UInt16 DataLen, ref UInt32 Value);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_log_state(UInt16 CardNo, UInt16 chan, ref UInt32 state);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_driver_reset(UInt16 CardNo, UInt16 axis);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_offset_pos(UInt16 CardNo, UInt16 axis, double offset_pos);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_offset_pos(UInt16 CardNo, UInt16 axis, ref double offset_pos);
        //Çå³ırtex¾ø¶ÔÖµ±àÂëÆ÷µÄ¶àÈ¦Öµ
        [DllImport("LTDMC.dll")]
        public static extern short nmc_clear_abs_driver_multi_cycle(UInt16 CardNo, UInt16 axis);
        //---------------------------EtherCAT IOÀ©Õ¹Ä£¿é²Ù×÷Ö¸Áî----------------------
        //ÉèÖÃioÊä³ö32Î»×ÜÏßÀ©Õ¹
        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_outport_extern(UInt16 CardNo, UInt16 Channel, UInt16 NoteID, UInt16 portno, UInt32 outport_val);
        //¶ÁÈ¡ioÊä³ö32Î»×ÜÏßÀ©Õ¹
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_outport_extern(UInt16 CardNo, UInt16 Channel, UInt16 NoteID, UInt16 portno, ref UInt32 outport_val);
        //¶ÁÈ¡ioÊäÈë32Î»×ÜÏßÀ©Õ¹
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_inport_extern(UInt16 CardNo, UInt16 Channel, UInt16 NoteID, UInt16 portno, ref UInt32 inport_val);
        //ÉèÖÃioÊä³ö
        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_outbit_extern(UInt16 CardNo, UInt16 Channel, UInt16 NoteID, UInt16 IoBit, UInt16 IoValue);
        //¶ÁÈ¡ioÊä³ö
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_outbit_extern(UInt16 CardNo, UInt16 Channel, UInt16 NoteID, UInt16 IoBit, ref UInt16 IoValue);
        //¶ÁÈ¡ioÊäÈë
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_inbit_extern(UInt16 CardNo, UInt16 Channel, UInt16 NoteID, UInt16 IoBit, ref UInt16 IoValue);
        
        //·µ»Ø×î½ü´íÎóÂë
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_current_fieldbus_state_info(UInt16 CardNo, UInt16 Channel, ref UInt16 Axis, ref UInt16 ErrorType, ref UInt16 SlaveAddr, ref UInt32 ErrorFieldbusCode);
        // ·µ»ØÀúÊ·´íÎóÂë
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_detail_fieldbus_state_info(UInt16 CardNo, UInt16 Channel, UInt32 ReadErrorNum, ref UInt32 TotalNum, ref UInt32 ActualNum, UInt16[] Axis, UInt16[] ErrorType, UInt16[] SlaveAddr, 
UInt32[] ErrorFieldbusCode);
        //Æô¶¯²É¼¯
        [DllImport("LTDMC.dll")]
        public static extern short nmc_start_pdo_trace(UInt16 CardNo, UInt16 Channel, UInt16 SlaveAddr, UInt16 Index_Num, UInt32 Trace_Len, UInt16[] Index, UInt16[] Sub_Index);
        //»ñÈ¡²É¼¯²ÎÊı
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_pdo_trace(UInt16 CardNo, UInt16 Channel, UInt16 SlaveAddr, ref UInt16 Index_Num, ref UInt32 Trace_Len, UInt16[] Index, UInt16[] Sub_Index);
        //ÉèÖÃ´¥·¢²É¼¯²ÎÊı
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_pdo_trace_trig_para(UInt16 CardNo, UInt16 Channel, UInt16 SlaveAddr, UInt16 Trig_Index, UInt16 Trig_Sub_Index, int Trig_Value, UInt16 Trig_Mode);
        //»ñÈ¡´¥·¢²É¼¯²ÎÊı
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_pdo_trace_trig_para(UInt16 CardNo, UInt16 Channel, UInt16 SlaveAddr, ref UInt16 Trig_Index, ref UInt16 Trig_Sub_Index, ref int Trig_Value, ref UInt16 Trig_Mode);
        //²É¼¯Çå³ı
        [DllImport("LTDMC.dll")]
        public static extern short nmc_clear_pdo_trace_data(UInt16 CardNo, UInt16 Channel, UInt16 SlaveAddr);
        //²É¼¯Í£Ö¹
        [DllImport("LTDMC.dll")]
        public static extern short nmc_stop_pdo_trace(UInt16 CardNo, UInt16 Channel, UInt16 SlaveAddr);
        //²É¼¯Êı¾İ¶ÁÈ¡
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_pdo_trace_data(UInt16 CardNo, UInt16 Channel, UInt16 SlaveAddr, UInt32 StartAddr, UInt32 Readlen, ref UInt32 ActReadlen, Byte[] Data);
        //ÒÑ²É¼¯¸öÊı
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_pdo_trace_num(UInt16 CardNo, UInt16 Channel, UInt16 SlaveAddr, ref UInt32 Data_num, ref UInt32 Size_of_each_bag);
        //²É¼¯×´Ì¬
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_pdo_trace_state(UInt16 CardNo, UInt16 Channel, UInt16 SlaveAddr, ref UInt16 Trace_state);
        //×ÜÏß×¨ÓÃ
        [DllImport("LTDMC.dll")]
        public static extern short nmc_reset_canopen(UInt16 CardNo);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_reset_rtex(UInt16 CardNo);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_reset_etc(UInt16 CardNo);
        //×ÜÏß´íÎó´¦ÀíÅäÖÃ
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_fieldbus_error_switch(UInt16 CardNo, UInt16 channel, UInt16 data);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_fieldbus_error_switch(UInt16 CardNo, UInt16 channel, ref UInt16 data);

        ////ÅäÖÃCSTÇĞ»»µ½CSPºó£¬ÓÉÓÚÇı¶¯Æ÷²»ÄÜ¼°Ê±Í¬²½Ö÷Õ¾Ä¿±êÎ»ÖÃ£¬ÑÓÊ±Ê±¼äÄÚÖ÷Õ¾¼ÌĞøÍ¬²½Çı¶¯Æ÷Êµ¼ÊÎ»ÖÃ£¬ÒÑÈ¡Ïû¸Ã¹¦ÄÜ
        //[DllImport("LTDMC.dll")]
        //public static extern short nmc_torque_set_delay_cycle(ushort CardNo, ushort axis, int delay_cycle);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_torque_move(UInt16 CardNo, UInt16 axis, int Torque, UInt16 PosLimitValid, double PosLimitValue, UInt16 PosMode);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_change_torque(UInt16 CardNo, UInt16 axis, int Torque);
        //¶ÁÈ¡×ª¾Ø´óĞ¡
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_torque(UInt16 CardNo, UInt16 axis, ref int Torque);
        //modbusº¯Êı
        [DllImport("LTDMC.dll")]
        public static extern short dmc_modbus_active_COM1(UInt16 id, string COMID,int speed, int bits, int check, int stop);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_modbus_active_COM2(UInt16 id, string COMID, int speed, int bits, int check, int stop);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_modbus_active_ETH(UInt16 id, UInt16 port);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_modbus_0x(UInt16 CardNo, UInt16 start, UInt16 inum, byte[] pdata);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_modbus_0x(UInt16 CardNo, UInt16 start, UInt16 inum, byte[] pdata);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_modbus_4x(UInt16 CardNo, UInt16 start, UInt16 inum, UInt16[] pdata);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_modbus_4x(UInt16 CardNo, UInt16 start, UInt16 inum, UInt16[] pdata);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_modbus_4x_float(UInt16 CardNo, UInt16 start, UInt16 inum, float[] pdata);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_modbus_4x_float(UInt16 CardNo, UInt16 start, UInt16 inum, float[] pdata);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_modbus_4x_int(UInt16 CardNo, UInt16 start, UInt16 inum, int[] pdata);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_modbus_4x_int(UInt16 CardNo, UInt16 start, UInt16 inum, int[] pdata);
        //±£Áô
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_line_io_union(UInt16 CardNo,UInt16 Crd,UInt16 AxisNum,UInt16[] AxisList,double[] pPosList,UInt16 posi_mode,UInt16 bitno,UInt16 on_off,double io_value,UInt16 io_mode,UInt16 MapAxis
,UInt16 pos_source,double ReverseTime,long mark);
        //ÉèÖÃ±àÂëÆ÷·½Ïò£¨ÊÊÓÃÓÚDMC3000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_encoder_dir(UInt16 CardNo, UInt16 axis,UInt16 dir);
        
        //Ô²»¡ÇøÓòÈíÏŞÎ»£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_arc_zone_limit_config(UInt16 CardNo, UInt16[] AxisList, UInt16 AxisNum, double[] Center, double Radius, UInt16 Source,UInt16 StopMode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_arc_zone_limit_config(UInt16 CardNo, UInt16[] AxisList, ref UInt16 AxisNum, double[] Center, ref double Radius, ref UInt16 Source,ref UInt16 StopMode);
        //Ô²ĞÎÇøÓòÈíÏŞÎ»unit£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_arc_zone_limit_config_unit(ushort CardNo, ushort[] AxisList, ushort AxisNum, double[] Center, double Radius, ushort Source, ushort StopMode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_arc_zone_limit_config_unit(ushort CardNo, ushort[] AxisList, ref ushort AxisNum, double[] Center, ref double Radius, ref ushort Source, ref ushort StopMode);
        //²éÑ¯ÏàÓ¦ÖáµÄ×´Ì¬£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_arc_zone_limit_axis_status(UInt16 CardNo, UInt16 axis);
        //Ô²ĞÎÏŞÎ»Ê¹ÄÜ£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_arc_zone_limit_enable(UInt16 CardNo, UInt16 enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_arc_zone_limit_enable(UInt16 CardNo, ref UInt16 enable);
        
        //¿ØÖÆ¿¨½ÓÏßºĞ¶ÏÏßºóÊÇ·ñ³õÊ¼»¯Êä³öµçÆ½
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_output_status_repower(UInt16 CardNo, UInt16 enable);
        //¾É½Ó¿Ú£¨ÈíÆô¶¯£©£¬²»Ê¹ÓÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_t_pmove_extern_softlanding(UInt16 CardNo, UInt16 axis, double MidPos, double TargetPos, double start_Vel, double Max_Vel, double stop_Vel, UInt32 delay_ms, double Max_Vel2, double 
stop_vel2, double acc_time, double dec_time, UInt16 posi_mode);
        //±£Áô
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_add_point_XD(UInt16 CardNo, UInt16 cmp, long pos, UInt16 dir, UInt16 action, UInt32 actpara, long startPos);//Îøµç¶¨ÖÆ±È½Ïº¯Êı
        
        //---------------------------ORGÊäÈë´¥·¢ÔÚÏß±äËÙ±äÎ»----------------------
        //ÅäÖÃORGÊäÈë´¥·¢ÔÚÏß±äËÙ±äÎ»£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pmove_change_pos_speed_config(UInt16 CardNo, UInt16 axis, double tar_vel, double tar_rel_pos, UInt16 trig_mode, UInt16 source);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pmove_change_pos_speed_config(UInt16 CardNo, UInt16 axis, ref double tar_vel, ref double tar_rel_pos, ref UInt16 trig_mode, ref UInt16 source);
        //ORGÊäÈë´¥·¢ÔÚÏß±äËÙ±äÎ»Ê¹ÄÜ£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pmove_change_pos_speed_enable(UInt16 CardNo, UInt16 axis, UInt16 enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pmove_change_pos_speed_enable(UInt16 CardNo, UInt16 axis, ref UInt16 enable);
        //¶ÁÈ¡ORGÊäÈë´¥·¢ÔÚÏß±äËÙ±äÎ»µÄ×´Ì¬  trig_num ´¥·¢´ÎÊı£¬trig_pos ´¥·¢Î»ÖÃ£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pmove_change_pos_speed_state(ushort CardNo, ushort axis, ref ushort trig_num, double[] trig_pos);
        //IO±äËÙ±äÎ»£¬ÅäÖÃioÊäÈë¿Ú£¨ÊÊÓÃÓÚEtherCAT×ÜÏßÏµÁĞ¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pmove_change_pos_speed_inbit(ushort CardNo, ushort axis, ushort inbit, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pmove_change_pos_speed_inbit(ushort CardNo, ushort axis, ref ushort inbit, ref ushort enable);
        //±£Áô
        [DllImport("LTDMC.dll")]
        public static extern short dmc_compare_add_point_extend(UInt16 CardNo, UInt16 axis, long pos, UInt16 dir, UInt16 action, UInt16 para_num, ref UInt32 actpara, UInt32 compare_time);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_cmd_position(UInt16 CardNo, UInt16 axis, ref double pos);
        //Âß¼­²ÉÑùÅäÖÃ£¨ÄÚ²¿Ê¹ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_logic_analyzer_config(UInt16 CardNo, UInt16 channel, UInt32 SampleFre, UInt32 SampleDepth, UInt16 SampleMode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_start_logic_analyzer(UInt16 CardNo, UInt16 channel, UInt16 enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_logic_analyzer_counter(UInt16 CardNo, UInt16 channel, ref UInt32 counter);
        //20190923ĞŞ¸Äkg¶¨ÖÆº¯Êı½Ó¿Ú£¨¿Í»§¶¨ÖÆ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_inbit_append(UInt16 CardNo, UInt16 bitno);//¶ÁÈ¡ÊäÈë¿ÚµÄ×´Ì¬
        [DllImport("LTDMC.dll")]
        public static extern short dmc_write_outbit_append(UInt16 CardNo, UInt16 bitno, UInt16 on_off);//ÉèÖÃÊä³ö¿ÚµÄ×´Ì¬
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_outbit_append(UInt16 CardNo, UInt16 bitno);//¶ÁÈ¡Êä³ö¿ÚµÄ×´Ì¬
        [DllImport("LTDMC.dll")]
        public static extern UInt32 dmc_read_inport_append(UInt16 CardNo, UInt16 portno);//¶ÁÈ¡ÊäÈë¶Ë¿ÚµÄÖµ
        [DllImport("LTDMC.dll")]
        public static extern UInt32 dmc_read_outport_append(UInt16 CardNo, UInt16 portno);//¶ÁÈ¡Êä³ö¶Ë¿ÚµÄÖµ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_write_outport_append(UInt16 CardNo, UInt16 portno, UInt32 port_value);//ÉèÖÃËùÓĞÊä³ö¶Ë¿ÚµÄÖµ

        //---------------------------ÍÖÔ²²å²¹¼°ÇĞÏò¸úËæ----------------------
        // ÉèÖÃ×ø±êÏµÇĞÏò¸úËæ£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_tangent_follow(UInt16 CardNo, UInt16 Crd, UInt16 axis, UInt16 follow_curve, UInt16 rotate_dir, double degree_equivalent);
        // »ñÈ¡Ö¸¶¨×ø±êÏµÇĞÏò¸úËæ²ÎÊı£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_tangent_follow_param(UInt16 CardNo, UInt16 Crd, ref UInt16 axis, ref UInt16 follow_curve, ref UInt16 rotate_dir, ref double degree_equivalent);
        // È¡Ïû×ø±êÏµ¸úËæ£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_disable_follow_move(UInt16 CardNo, UInt16 Crd);
        // ÍÖÔ²²å²¹£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_ellipse_move(UInt16 CardNo, UInt16 Crd, UInt16 axisNum, UInt16[] Axis_List, double[] Target_Pos, double[] Cen_Pos, double A_Axis_Len, double B_Axis_Len, UInt16 Dir, UInt16 Pos_Mode);

        //---------------------------¿´ÃÅ¹·¹¦ÄÜ----------------------
        //ÉèÖÃ¿´ÃÅ¿Ú´¥·¢ÏìÓ¦ÊÂ¼ş£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_watchdog_action_event(UInt16 CardNo, UInt16 event_mask);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_watchdog_action_event(UInt16 CardNo, ref UInt16 event_mask);
        //Ê¹ÄÜ¿´ÃÅ¿Ú±£»¤»úÖÆ£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_watchdog_enable(UInt16 CardNo, double timer_period, UInt16 enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_watchdog_enable(UInt16 CardNo, ref double timer_period, ref UInt16 enable);
        //¸´Î»¿´ÃÅ¹·¶¨Ê±Æ÷£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_reset_watchdog_timer(UInt16 CardNo);

        //io¶¨ÖÆ¹¦ÄÜ£¨¶¨ÖÆÀà£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_io_check_control(UInt16 CardNo, UInt16 sensor_in_no, UInt16 check_mode, UInt16 A_out_no, UInt16 B_out_no, UInt16 C_out_no, UInt16 output_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_io_check_control(UInt16 CardNo, ref UInt16 sensor_in_no, ref UInt16 check_mode, ref UInt16 A_out_no, ref UInt16 B_out_no, ref UInt16 C_out_no, ref UInt16 output_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_stop_io_check_control(UInt16 CardNo);

        //ÉèÖÃÏŞÎ»·´ÕÒÆ«ÒÆ¾àÀë£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_el_ret_deviation(UInt16 CardNo, UInt16 axis, UInt16 enable, double deviation);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_el_ret_deviation(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref double deviation);

        //Á½ÖáÎ»ÖÃµş¼Ó£¬¸ßËÙ±È½Ï¹¦ÄÜ£¨²âÊÔÊ¹ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_set_config_overlap(UInt16 CardNo, UInt16 hcmp, UInt16 axis, UInt16 cmp_source, UInt16 cmp_logic, Int32 time, UInt16 axis_num, UInt16 aux_axis, UInt16 aux_source);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_get_config_overlap(UInt16 CardNo, UInt16 hcmp, ref UInt16 axis, ref UInt16 cmp_source, ref UInt16 cmp_logic, ref Int32 time, ref UInt16 axis_num, ref UInt16 aux_axis, ref UInt16 
aux_source);
        
        //Æô¶¯»òÕß¹Ø±ÕRTCP¹¦ÄÜ,ºóĞøÌí¼Ó

        //ÂİĞı²å²¹(²âÊÔÊ¹ÓÃ£¬DMC5000/5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨)
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_helix_move_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AixsList, double[] StartPos, double[] TargetPos, UInt16 Arc_Dir, int Circle, UInt16 mode, int mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_helix_move_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] StartPos, double[] TargetPos, UInt16 Arc_Dir, int Circle, UInt16 mode);

        //PDO»º´æ20190715£¨ÄÚ²¿Ê¹ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pdo_buffer_enter(UInt16 CardNo, UInt16 axis);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pdo_buffer_stop(UInt16 CardNo, UInt16 axis);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pdo_buffer_clear(UInt16 CardNo, UInt16 axis);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pdo_buffer_run_state(UInt16 CardNo,UInt16 axis, ref int RunState, ref int Remain, ref int NotRunned, ref int Runned);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pdo_buffer_add_data(UInt16 CardNo,UInt16 axis, int size, int[] data_table);       
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pdo_buffer_start_multi(UInt16 CardNo, UInt16 AxisNum, UInt16[] AxisList, UInt16[] ResultList);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pdo_buffer_pause_multi(UInt16 CardNo, UInt16 AxisNum, UInt16[] AxisList, UInt16[] ResultList);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pdo_buffer_stop_multi(UInt16 CardNo, UInt16 AxisNum, UInt16[] AxisList, UInt16[] ResultList);
        //[DllImport("LTDMC.dll")]
        //public static extern short dmc_pdo_buffer_add_data_multi(UInt16 CardNo, UInt16 AxisNum, UInt16[] AxisList, int size, int[][] data_table);
        //±£Áô
        [DllImport("LTDMC.dll")]
        public static extern short dmc_calculate_arccenter_3point(double[] start_pos, double[] mid_pos, double[] target_pos, double[] cen_pos);

        //---------------------Ö¸Áî»º´æÃÅĞÍÔË¶¯------------------
        //Ö¸Áî»º´æÃÅĞÍÔË¶¯£¨ÊÊÓÃÓÚDMC3000/5000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_set_muti_profile_unit(ushort card, ushort group, ushort axis_num, ushort[] axis_list, double[] start_vel, double[] max_vel, double[] tacc, double[] tdec, double[] stop_vel);
//Á½ÖáËÙ¶ÈÉèÖÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_set_profile_unit(ushort card, ushort group, ushort axis, double start_vel, double max_vel, double tacc, double tdec, double stop_vel);//µ¥ÖáËÙ¶ÈÉèÖÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_sigaxis_moveseg_data(ushort card, ushort group, ushort axis, double Target_pos, ushort process_mode, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_sigaxis_move_twoseg_data(ushort card, ushort group, ushort axis, double Target_pos, double second_pos, double second_vel, double second_endvel, ushort process_mode, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_mutiaxis_moveseg_data(ushort card, ushort group, ushort axisnum, ushort[] axis_list, double[] Target_pos, ushort process_mode, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_mutiaxis_move_twoseg_data(ushort card, ushort group, ushort axisnum, ushort[] axis_list, double[] Target_pos, double[] second_pos, double[] second_vel, double[] second_endvel, 
ushort process_mode, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_ioTrig_movseg_data(ushort card, ushort group, ushort axisNum, ushort[] axisList, double[] Target_pos, ushort process_mode, ushort trigINbit, ushort trigINstate, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_mutiposTrig_movseg_data(ushort card, ushort group, ushort axis, double Target_pos, ushort process_mode, ushort trigaxisNum, ushort[] trigAxisList, double[] trigPos, ushort[] 
trigPosType, ushort[] trigMode, uint mark);//Î»ÖÃ´¥·¢ÒÆ¶¯
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_mutiposTrig_mov_twoseg_data(ushort card, ushort group, ushort axis, double Target_pos, double softland_pos, double softland_vel, double softland_endvel, ushort process_mode, 
ushort trigAxisNum, ushort[] trigAxisList, double[] trigPos, ushort[] trigPosType, ushort[] trigMode, uint mark);//¶àÖáÎ»ÖÃ´¥·¢ÒÆ¶¯
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_upseg_data(ushort card, ushort group, ushort axis, double Target_pos, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_up_twoseg_data(ushort card, ushort group, ushort axis, double Target_pos, double second_pos, double second_vel, double second_endvel, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_ioPosTrig_movseg_data(ushort card, ushort group, ushort axisNum, ushort[] axisList, double[] Target_pos, ushort process_mode, ushort trigAxis, double trigPos, ushort trigPosType, 
ushort trigMode, ushort TrigINNum, ushort[] trigINList, ushort[] trigINstate, uint mark);//Î»ÖÃ+io´¥·¢ÒÆ¶¯
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_ioPosTrig_mov_twoseg_data(ushort card, ushort group, ushort axisNum, ushort[] axisList, double[] Target_pos, double[] second_pos, double[] second_vel, double[] second_endvel, 
ushort process_mode, ushort trigAxis, double trigPos, ushort trigPosType, ushort trigMode, ushort TrigINNum, ushort[] trigINList, ushort[] trigINstate, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_posTrig_movseg_data(ushort card, ushort group, ushort axisNum, ushort[] axisList, double[] Target_pos, ushort process_mode, ushort trigAxis, double trigPos, ushort trigPosType, 
ushort trigMode, uint mark);//Î»ÖÃ´¥·¢ÒÆ¶¯
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_posTrig_mov_twoseg_data(ushort card, ushort group, ushort axisNum, ushort[] axisList, double[] Target_pos, double[] second_pos, double[] second_vel, double[] second_endvel, ushort
 process_mode, ushort trigAxis, double trigPos, ushort trigPosType, ushort trigMode, uint mark);//Î»ÖÃ´¥·¢ÒÆ¶¯
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_ioPosTrig_down_seg_data(ushort card, ushort group, ushort axis, double safePos, double Target_pos, ushort trigAxisNum, ushort[] trigAxisList, double[] trigPos, ushort[] 
trigPosType, ushort[] trigMode, ushort trigIN, ushort trigINstate, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_ioPosTrig_down_twoseg_data(ushort card, ushort group, ushort axis, double safePos, double Target_pos, double second_pos, double second_vel, double second_endvel, ushort 
trigAxisNum, ushort[] trigAxisList, double[] trigPos, ushort[] trigPosType, ushort[] trigMode, ushort trigIN, ushort trigINstate, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_posTrig_down_seg_data(ushort card, ushort group, ushort axis, double safePos, double Target_pos, ushort trigAxisNum, ushort[] trigAxisList, double[] trigPos, ushort[] trigPosType,
 ushort[] trigMode, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_posTrig_down_twoseg_data(ushort card, ushort group, ushort axis, double safePos, double Target_pos, double second_pos, double second_vel, double second_endvel, ushort trigAxisNum,
 ushort[] trigAxisList, double[] trigPos, ushort[] trigPosType, ushort[] trigMode, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_posTrig_down_seg_cmd_data(ushort card, ushort group, ushort axis, double safePos, double Target_pos, ushort trigAxisNum, ushort[] trigAxisList, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_posTrig_down_twoseg_cmd_data(ushort card, ushort group, ushort axis, double safePos, double Target_pos, double second_pos, double second_vel, double second_endvel, ushort 
trigAxisNum, ushort[] trigAxisList, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_mutiposTrig_singledown_seg_data(ushort card, ushort group, ushort axis, double safePos, double Target_pos, ushort process_mode, ushort trigAxisNum, ushort[] trigAxisList, double[]
 trigPos, ushort[] trigPosType, ushort[] trigMode, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_mutiposTrig_mutidown_seg_data(ushort card, ushort group, ushort axisnum, ushort[] axis_list, double[] safePos, double[] Target_pos, ushort process_mode, ushort trigAxisNum, ushort
[] trigAxisList, double[] trigPos, ushort[] trigPosType, ushort[] trigMode, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_posTrig_outbit(ushort card, ushort group, ushort bitno, ushort on_off, ushort ahead_axis, double ahead_value, ushort ahead_PosType, ushort ahead_Mode, uint mark);//Î»ÖÃ´¥·¢IOÊä³ö
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_mutiposTrig_outbit(ushort card, ushort group, ushort bitno, ushort on_off, ushort process_mode, ushort trigaxisNum, ushort[] trigAxisList, double[] trigPos, ushort[] trigPosType, 
ushort[] trigMode, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_immediate_write_outbit(ushort card, ushort group, ushort bitno, ushort on_off, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_wait_input(ushort card, ushort group, ushort bitno, ushort on_off, double time_out, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_delay_time(ushort card, ushort group, double delay_time, uint mark);//ÑÓÊ±Ö¸Áî
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_get_run_state(ushort card, ushort group, ref ushort state, ref ushort enable, ref uint stop_reason, ref ushort trig_phase, ref uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_open_list(ushort card, ushort group, ushort axis_num, ushort[] axis_list);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_close_list(ushort card, ushort group);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_start_list(ushort card, ushort group);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_stop_list(ushort card, ushort group, ushort stopMode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_pause_list(ushort card, ushort group, ushort stopMode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_set_encoder_error_allow(ushort card, ushort group, double allow_error);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_get_encoder_error_allow(ushort card, ushort group, ref double allow_error);

        //¶ÁÈ¡ËùÓĞADÊäÈë£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_ad_input_all(ushort CardNo, ref double Vout);
        //Á¬Ğø²å²¹ÔİÍ£ºóÊ¹ÓÃpmove£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_pmove_unit_pausemode(ushort CardNo, ushort axis, double TargetPos, double Min_Vel, double Max_Vel, double stop_Vel, double acc, double dec, double smooth_time, ushort posi_mode);
        //Á¬Ğø²å²¹ÔİÍ£Ê¹ÓÃpmoveºó£¬»Øµ½ÔİÍ£Î»ÖÃ£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_return_pausemode(ushort CardNo, ushort Crd, ushort axis);
        //¼ìÑé½ÓÏßºĞÊÇ·ñÖ§³ÖÍ¨Ñ¶Ğ£Ñé£¨ÊÊÓÃÓÚDMC3000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_check_if_crc_support(ushort CardNo);

        //ÖáÅö×²¼ì²â¹¦ÄÜ½Ó¿Ú £¨ÊÊÓÃÓÚDMC3000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_conflict_config(ushort CardNo, ushort[] axis_list, ushort[] axis_depart_dir, double home_dist, double conflict_dist, ushort stop_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_conflict_config(ushort CardNo, ushort[] axis_list, ushort[] axis_depart_dir, ref double home_dist, ref double conflict_dist, ref ushort stop_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_axis_conflict_config_en(ushort CardNo, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_conflict_config_en(ushort CardNo, ref ushort enable);
       
        //Îï¼ş·Ö¼ğ¼ÓÍ¨µÀ,·Ö¼ğ¹Ì¼ş×¨ÓÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_close_ex(ushort CardNo, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_start_ex(ushort CardNo, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_init_config_ex(ushort CardNo, ushort cameraCount, int[] pCameraPos, ushort[] pCamIONo, UInt32 cameraTime, ushort cameraTrigLevel, ushort blowCount, int[] pBlowPos, ushort[] 
pBlowIONo, UInt32 blowTime, ushort blowTrigLevel, ushort axis, ushort dir, ushort checkMode, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_camera_trig_count_ex(ushort CardNo, ushort cameraNum, UInt32 cameraTrigCnt, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_get_camera_trig_count_ex(ushort CardNo, ushort cameraNum, ref UInt32 pCameraTrigCnt, ushort count, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_blow_trig_count_ex(ushort CardNo, ushort blowNum, UInt32 blowTrigCnt, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_get_blow_trig_count_ex(ushort CardNo, ushort blowNum, ref UInt32 pBlowTrigCnt, ushort count, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_get_camera_config_ex(ushort CardNo, ushort index, ref int pos, ref UInt32 trigTime, ref ushort ioNo, ref ushort trigLevel, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_get_blow_config_ex(ushort CardNo, ushort index, ref int pos, ref UInt32 trigTime, ref ushort ioNo, ref ushort trigLevel, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_get_blow_status_ex(ushort CardNo, ref UInt32 trigCntAll, ref ushort trigMore, ref ushort trigLess, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_trig_blow_ex(ushort CardNo, ushort blowNum, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_blow_enable_ex(ushort CardNo, ushort blowNum, ushort enable, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_piece_config_ex(ushort CardNo, UInt32 maxWidth, UInt32 minWidth, UInt32 minDistance, UInt32 minTimeIntervel, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_get_piece_status_ex(ushort CardNo, ref UInt32 pieceFind, ref UInt32 piecePassCam, ref UInt32 dist2next, ref UInt32 pieceWidth, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_cam_trig_phase_ex(ushort CardNo, ushort blowNo, double coef, ushort sortModuleNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sorting_set_blow_trig_phase_ex(ushort CardNo, ushort blowNo, double coef, ushort sortModuleNo);
        //»ñÈ¡·Ö¼ğÖ¸ÁîÊıÁ¿º¯Êı
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_sortdev_blow_cmd_cnt(ushort CardNo, ushort blowDevNum, ref long cnt);
        //»ñÈ¡Î´´¦ÀíÖ¸ÁîÊıÁ¿º¯Êıº¯Êı
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_sortdev_blow_cmderr_cnt(ushort CardNo, ushort blowDevNum, ref long errCnt);
        //·Ö¼ğ¶ÓÁĞ×´Ì¬
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_sortqueue_status(ushort CardNo, ref long curSorQueueLen, ref long passCamWithNoCmd);

        // ÍÖÔ²Á¬Ğø²å²¹£¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨¡¢E5032×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_ellipse_move_unit(ushort CardNo, ushort Crd,ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Cen_Pos, double A_Axis_Len, double B_Axis_Len, ushort Dir, ushort 
Pos_Mode,long mark);
        //»ñÈ¡Öá×´Ì¬º¯Êı£¨Ô¤Áô£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_status_advance(ushort CardNo, ushort axis_no, ushort motion_no, ref ushort axis_plan_state, ref UInt32 ErrPlulseCnt, ref ushort fpga_busy);
        //Á¬Ğø²å²¹vmove£¨DMC5000ÏµÁĞ¿¨ÊÜÏŞÊ¹ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_vmove_unit(ushort CardNo, ushort Crd, ushort axis, double vel, double acc, ushort dir, Int32 imark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_vmove_stop(ushort CardNo, ushort Crd, ushort axis, double dec, Int32 imark);

        //---------------------¶ÁĞ´µôµç±£³ÖÇø------------------//
        //Ğ´Èë×Ö·ûÊı¾İµ½¶Ïµç±£³ÖÇø£¨DMC3000/5000ÏµÁĞ¿¨ÊÜÏŞÊ¹ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_persistent_reg_byte(ushort CardNo, uint start, uint inum, byte[] pdata);
        //´Ó¶Ïµç±£³ÖÇø¶ÁÈ¡Ğ´ÈëµÄ×Ö·û£¨DMC3000/5000ÏµÁĞ¿¨ÊÜÏŞÊ¹ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_persistent_reg_byte(ushort CardNo, uint start, uint inum, byte[] pdata);
        //Ğ´Èë¸¡µãĞÍÊı¾İµ½¶Ïµç±£³ÖÇø£¨DMC3000/5000ÏµÁĞ¿¨ÊÜÏŞÊ¹ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_persistent_reg_float(ushort CardNo, uint start, uint inum, float[] pdata);
        //´Ó¶Ïµç±£³ÖÇø¶ÁÈ¡Ğ´ÈëµÄ¸¡µãĞÍÊı¾İ£¨DMC3000/5000ÏµÁĞ¿¨ÊÜÏŞÊ¹ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_persistent_reg_float(ushort CardNo, uint start, uint inum, float[] pdata);
        //Ğ´ÈëÕûĞÍÊı¾İµ½¶Ïµç±£³ÖÇø£¨DMC3000/5000ÏµÁĞ¿¨ÊÜÏŞÊ¹ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_persistent_reg_int(ushort CardNo, uint start, uint inum, int[] pdata);
        //´Ó¶Ïµç±£³ÖÇø¶ÁÈ¡Ğ´ÈëµÄÕûĞÍÊı¾İ£¨DMC3000/5000ÏµÁĞ¿¨ÊÜÏŞÊ¹ÓÃ£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_persistent_reg_int(ushort CardNo, uint start, uint inum, int[] pdata);
        //----------------------------------------------------//

        //EtherCAT×ÜÏß¸´Î»IOÄ£¿éÊä³ö±£³Ö¿ª¹ØÉèÖÃ202001£¨ÊÊÓÃÓÚËùÓĞEtherCAT×ÜÏß¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_slave_output_retain(ushort CardNo, ushort Enable);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_slave_output_retain(ushort CardNo, ref ushort Enable);

        //Öá²ÎÊıÅäÖÃĞ´flash£¬ÊµÏÖ¶Ïµç±£´æ¼±Í£ĞÅºÅÅäÖÃ£¨ÊÊÓÃÓÚDMC3000ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_persistent_param_config(ushort CardNo, ushort axis, uint item);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_persistent_param_config(ushort CardNo, ushort axis, ref uint item);               

        //¶ÁÈ¡ÔËĞĞÊ±ÊÇÆô¶¯Õı³£¹Ì¼ş»¹ÊÇ±¸·İ¹Ì¼ş£¨ÊÊÓÃÓÚDMC3000/5000/5X10ÏµÁĞÂö³å¿¨£©
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_firmware_boot_type(ushort CardNo, ref ushort boot_type);

        /**************************ÖĞ¶Ï¹¦ÄÜ £¨ÊÊÓÃÓÚDMC5X10ÏµÁĞÂö³å¿¨£©************************/
        //¿ªÆô¿ØÖÆ¿¨ÖĞ¶Ï¹¦ÄÜ
        [DllImport("LTDMC.dll")]
        public static extern uint dmc_int_enable(ushort CardNo, DMC3K5K_OPERATE funcIntHandler, IntPtr operate_data);
        //½ûÖ¹¿ØÖÆ¿¨µÄÖĞ¶Ï
        [DllImport("LTDMC.dll")]
        public static extern uint dmc_int_disable(ushort CardNo);
        //ÉèÖÃ/¶ÁÈ¡Ö¸¶¨¿ØÖÆ¿¨ÖĞ¶ÏÍ¨µÀÊ¹ÄÜ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_intmode_enable(ushort Cardno,ushort Intno,ushort Enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_intmode_enable(ushort Cardno,ushort Intno,ref ushort Status);
        //ÉèÖÃ/¶ÁÈ¡Ö¸¶¨¿ØÖÆ¿¨ÖĞ¶ÏÅäÖÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_intmode_config(ushort Cardno,ushort Intno,ushort IntItem,ushort IntIndex,ushort IntSubIndex,ushort Logic);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_intmode_config(ushort Cardno,ushort Intno,ref ushort IntItem,ref ushort IntIndex,ref ushort IntSubIndex,ref ushort Logic);
        //¶ÁÈ¡Ö¸¶¨¿ØÖÆ¿¨ÖĞ¶ÏÍ¨µÀµÄÖĞ¶Ï×´Ì¬
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_int_status(ushort Cardno,ref uint IntStatus);
        //¸´Î»Ö¸¶¨¿ØÖÆ¿¨ÊäÈë¿ÚµÄÖĞ¶Ï
        [DllImport("LTDMC.dll")]
        public static extern short dmc_reset_int_status(ushort Cardno, ushort Intno);
        /**************************************************************************************/


        /******************************************2021.10.26 ¿ªÊ¼ĞŞ¸Ä**********************/

        ////20160105Ôö¼ÓĞÂËÙ¶ÈÇúÏßÒÔ¼ÓËÙ¶È ¼õËÙ¶È ¼õ¼õËÙ¶ÈÀ´±íÊ¾(Âö³å)
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_profile_extern(UInt16 CardNo, UInt16 axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Ajerk, double Djerk, double stop_vel);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_profile_extern(UInt16 CardNo, UInt16 axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double Ajerk, ref double Djerk, ref double stop_vel);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_delay_pwm_to_stop(UInt16 CardNo, UInt16 Crd, UInt16 pwmno, UInt16 on_off, double delay_time, double ReverseTime);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_profile_limit(UInt16 CardNo, UInt16 axis, double Max_Vel, double Max_Acc, double EvenTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_profile_limit(UInt16 CardNo, UInt16 axis,ref double Max_Vel,ref double Max_Acc,ref double EvenTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_vector_profile_limit(UInt16 CardNo, UInt16 Crd, double Max_Vel, double Max_Acc, double EvenTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_vector_profile_limit(UInt16 CardNo, UInt16 Crd,ref double Max_Vel,ref double Max_Acc,ref double EvenTime);





        //¶şÎ¬¸ßËÙÎ»ÖÃ±È½Ï»º´æ
        //1¡¢	ÆôÓÃ»º´æ·½Ê½Ìí¼Ó±È½ÏÎ»ÖÃ£º
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_fifo_set_mode(UInt16 CardNo, UInt16 hcmp, UInt16 fifo_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_fifo_get_mode(UInt16 CardNo, UInt16 hcmp,ref UInt16 fifo_mode);
        //2¡¢	¶ÁÈ¡Ê£Óà»º´æ×´Ì¬£¬ÉÏÎ»»úÍ¨¹ı´Ëº¯ÊıÅĞ¶ÏÊÇ·ñ¼ÌĞøÌí¼Ó±È½ÏÎ»ÖÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_fifo_get_state(UInt16 CardNo, UInt16 hcmp, ref long remained_points);
        //3¡¢	°´Êı×éµÄ·½Ê½ÅúÁ¿Ìí¼Ó±È½ÏÎ»ÖÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_fifo_add_point_unit(UInt16 CardNo, UInt16 hcmp, UInt16 num, double[] x_cmp_pos, double[] y_cmp_pos, UInt16[] cmp_outbit);
        //4¡¢	Çå³ı±È½ÏÎ»ÖÃ,Ò²»á°ÑFPGAµÄÎ»ÖÃÍ¬²½Çå³ıµô
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_fifo_clear_points(UInt16 CardNo, UInt16 hcmp);
        //Ìí¼Ó´óÊı¾İ£¬»á¶ÂÈûÒ»¶ÎÊ±¼ä£¬Ö¸µ¼Êı¾İÌí¼ÓÍê³É
        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_2d_fifo_add_table(UInt16 CardNo, UInt16 hcmp, UInt16 num, double[] x_cmp_pos, double[] y_cmp_pos);



        //ÃÅÔË¶¯
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_move_unit(UInt16 CardNo, UInt16 Crd, UInt16 axis_num, UInt16[] axis_list, double[] mid_pos, double[] target_pos, double[] saftpos, UInt16 pos_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_m_move_config(UInt16 CardNo, UInt16 Crd,ref UInt16 axis_num, UInt16[] axis_list, double[] mid_pos, double[] target_pos, double[] saftpos,ref UInt16 pos_mode);


        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_to_pci(UInt16 CardNo, UInt16 PortNum, UInt16 NodeNum);


        //TypeIndex:0~6  m_Averagetime ; Æ½¾ùÊ±¼ä m_Maxtime;×î´óÊ±¼ä uint64  m_Cycles;µ±Ç°Ê±¼ä
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_perline_time(UInt16 CardNo, UInt16 TypeIndex, ref int Averagetime, ref int Maxtime,ref UInt64 Cycles);



        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_axis_contrlmode(UInt16 CardNo, UInt16 Axis, long Contrlmode);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_axis_contrlmode(UInt16 CardNo, UInt16 Axis, ref long Contrlmode);



        // »ñÈ¡¿ØÖÆ¿¨´íÎóÂë
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_card_errcode(UInt16 CardNo, ref UInt16 Errcode);
        // Çå³ı¿ØÖÆ¿¨´íÎóÂë
        [DllImport("LTDMC.dll")]
        public static extern short nmc_clear_card_errcode(UInt16 CardNo);




        [DllImport("LTDMC.dll")]
        public static extern short nmc_start_log(UInt16 CardNo, UInt16 chan, UInt16 node, UInt16 Ifenable);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_log(UInt16 CardNo, UInt16 chan, UInt16 node,ref UInt32 data);


        //ĞÂ°æÃÅÔË¶¯
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_move_set_coodinate(UInt16 card, UInt16 liner, UInt16 axis_num, UInt16[] axis_list, UInt32 in_io_num, UInt16 trig_flag, UInt16 pos_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_move_get_coodinate_para(UInt16 card, UInt16 liner, ref UInt16 axis_num, UInt16[] axis_list, ref UInt32 in_io_num, ref UInt16 trig_flag, ref UInt16 pos_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_move_set_z_para(UInt16 card, UInt16 liner, double z_up_safe_pos, double z_up_target_pos, double z_down_safe_pos, double z_down_target_pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_move_get_z_para(UInt16 card, UInt16 liner, ref double z_up_safe_pos, ref double z_up_target_pos, ref double z_down_safe_pos, ref double z_down_target_pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_move_set_xy_para(UInt16 card, UInt16 liner, double x_first_safe_pos, double x_second_safe_pos, double x_target_pos, UInt16 y_num, double[] y_target_pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_move_get_xy_para(UInt16 card, UInt16 liner, ref double x_first_safe_pos, ref double x_second_safe_pos, ref double x_target_pos, UInt16 y_num, double[] y_target_pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_move_axes(UInt16 card, UInt16 liner);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_move_get_run_phase(UInt16 card, UInt16 liner,ref UInt16 run_phase);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_move_stop(UInt16 card, UInt16 liner, UInt16 stop_mode);






        //ÉèÖÃÎåÖá»úÆ÷µÄÀàĞÍ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_set_kinematic_type(ushort CardNo, ushort Crd, ushort machine_type);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_kinematic_type(ushort CardNo, ushort Crd, ref ushort machine_type);
        //Æô¶¯»òÕß¹Ø±ÕRTCP¹¦ÄÜ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_set_enable(ushort CardNo, ushort Crd, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_enable(ushort CardNo, ushort Crd, ref ushort enable);
        //ÉèÖÃAÖáµÄ×ø±êÔ­µãÏà¶ÔÓÚÇ°Ò»¸ö×ø±êÏµµÄÆ«ÒÆ, xyzµÄÆ«ÒÆa_offset[3]
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_set_vector_a(ushort CardNo, ushort Crd, double[] a_offset);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_vector_a(ushort CardNo, ushort Crd, double[] a_offset);
        //ÉèÖÃBÖáµÄ×ø±êÔ­µãÏà¶ÔÓÚÇ°Ò»¸ö×ø±êÏµµÄÆ«ÒÆ, xyzµÄÆ«ÒÆb_offset[3]
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_set_vector_b(ushort CardNo, ushort Crd, double[] b_offset);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_vector_b(ushort CardNo, ushort Crd, double[] b_offset);
        //ÉèÖÃCÖáµÄ×ø±êÔ­µãÏà¶ÔÓÚÇ°Ò»¸ö×ø±êÏµµÄÆ«ÒÆ, xyzµÄÆ«ÒÆc_offset[3]
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_set_vector_c(ushort CardNo, ushort Crd, double[] c_offset);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_vector_c(ushort CardNo, ushort Crd, double[] c_offset);
        //ÉèÖÃA£¬B£¬CÖáµÄÆ«ÒÆÎ»ÖÃ,
        //A,B,CÔÚ»Ø0ºó£¬ÔÙÒÆ¶¯µ½³õÊ¼×ËÌ¬µÄÎ»ÖÃ£¬Õâ¸öÊ±ºòµÄA/B/CµÄÆ«ÒÆ½Ç¶È£¬
        //Èç¹ûµ½ÁË³õÊ¼×ËÌ¬µÄÎ»ÖÃ²»½øĞĞÇå0£¬Õâ¸öÊ±ºò¾ÍÒªÉèÖÃÆ«ÒÆ½Ç¶È
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_set_rotate_joint_offset(ushort CardNo, ushort Crd, double A, double B, double C);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_rotate_joint_offset(ushort CardNo, ushort Crd, ref double A, ref double B, ref double C);
        //ÉèÖÃ¸÷ÖáµÄ·½ÏòÓë¹¤¼ş×ø±êÏµµÄ¹ØÏµ
        //ÖáÏà¶ÔÓÚ¹¤¼şµÄ·½Ïò£¬Èç¹ûÖáÕıÏòÔË¶¯µÄÊ±ºò£¬Çı¶¯µ¶¾ßÏà¶ÔÓÚ¹¤¼şÒ²ÊÇÕıÏòÔË¶¯µÄ£¬Éè¶¨Îª1
        //·ñÔòÉèÎª-1£¬³õÊ¼ÉèÖÃÎª1
        //dir[5],¶ÔÓ¦µÄÊÇX,Y,Z,Ğı×ªÖá1£¬Ğı×ªÖá2
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_set_joints_direction(ushort CardNo, ushort Crd, int[] dir);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_joints_direction(ushort CardNo, ushort Crd, int[] dir);
        //ÉèÖÃµ¶¾ß³¤¶È£¬ÔÚË«°ÚÍ·ÖĞÓĞ×÷ÓÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_set_tool_length(ushort CardNo, ushort Crd, double tool);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_tool_length(ushort CardNo, ushort Crd, ref double tool);
        //ÉèÖÃ´¿Ğı×ªÖáµÄËÙ¶È
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_set_max_rotate_param(ushort CardNo, ushort Crd, double rotate_speed, double rotate_acc);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_max_rotate_param(ushort CardNo, ushort Crd, ref double rotate_speed, ref double rotate_acc);
        //ÉèÖÃ¹¤¼şÔ­µãÆ«ÖÃÖµ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_set_workpiece_offset(ushort CardNo, ushort Crd, ushort workpiece_index, double[] offset);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_workpiece_offset(ushort CardNo, ushort Crd, ushort workpiece_index, double[] offset);
        //¶ÁÈ¡¹¤¼şÎ»ÖÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_actual_pos(ushort CardNo, ushort Crd, ushort AxisNum, double[] actual_pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_command_pos(ushort CardNo, ushort Crd, ushort AxisNum, double[] command_pos);
        //»úĞµ×ø±êÓë¹¤¼ş×ø±ê×ª»»
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_kinematics_forward(ushort CardNo, ushort Crd, double[] joint_pos, double[] axis_pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_kinematics_forward_ex(ushort CardNo, ushort Crd, double[] joint_pos, double[] axis_pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_kinematics_inverse(ushort CardNo, ushort Crd, double[] axis_pos, double[] joint_pos);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_set_workpiece_mode(ushort CardNo, ushort Crd, ushort enable, ushort workpiece_index);    //×ø±êÏµºÅ0-3£¬Ê¹ÄÜÄÄ¸ö¹¤¼ş×ø±êÏµ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_workpiece_mode(ushort CardNo, ushort Crd,ref ushort enable,ref ushort workpiece_index);    //×ø±êÏµºÅ0-3£¬Ê¹ÄÜÄÄ¸ö¹¤¼ş×ø±êÏµ


        //Î»ÖÃÊäÈëÊı¾İÀàĞÍ£º0-¹¤¼ş×ø±êÏµÎ»ÖÃ£¬1-»úĞµ×ø±êÏµÎ»ÖÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_set_position_type(ushort CardNo, ushort Crd, ushort position_type);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_position_type(ushort CardNo, ushort Crd, ref ushort position_type);




        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_open(ushort CardNo, ushort group, ushort axis_num, ushort[] axis_list);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_close(ushort CardNo, ushort group);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_start(ushort CardNo, ushort group);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_stop(ushort CardNo, ushort group, ushort stop_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_get_group_config(ushort CardNo, ushort group,ref ushort enable, ref ushort axis_num, ushort[] axis_list);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_get_group_run_info(ushort CardNo, ushort group, ref ushort enable, ref UInt32 array_id, ref UInt32 stop_reason, ref ushort trig_phase, ref ushort phase);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_add_cmd(ushort CardNo, ushort group, UInt32 index, ushort cmd_type, ushort ProcessMode, ushort Dim, ushort[] AxisList, double[] TargetPositionFirst,
            double[] m_TargetPositionSecond, ushort[] m_SoftlandFlag, double[] SoftLandVel, double[] SoftLandEndVel, ushort[] m_PosMode, double[] m_TrigAheadPos,
            ushort m_TrigFlag, ushort m_TrigAxisNum, ushort[] m_TrigAxislist, ushort[] m_TrigPosType, ushort[] m_TrigAxisPosRelationgship, double[] m_TrigAxisPos,
            ushort m_TrigIONum, ushort[] m_TrigIOState, ushort[] m_TrigINIOList, UInt32 m_DelayCmdTime, ushort m_IOList, ushort m_IOState, ushort m_TrigError);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_set_axis_profile(ushort CardNo, ushort group, ushort axis_num, ushort[] axis_list, double[] start_vel, double[] max_vel, double[] stop_vel, double[] tacc, double[] tdec);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_posTrig_torque_movseg_data(ushort CardNo, ushort group, ushort axis, double torque, double PosLimitValue, ushort PosLimitValid, ushort PosMode, ushort trigAxis, double trigPos, 
ushort trigPosType, ushort trigMode, UInt32 mark);//Î»ÖÃ´¥·¢×ª¾ØÒÆ¶¯


        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_temp_stop(ushort CardNo, ushort group, ushort stop_mode);












        //µç×Ó³İÂÖ¹¦ÄÜ

        [DllImport("LTDMC.dll")]
        public static extern short dmc_gear_in(ushort CardNo, ushort master_axis, ushort slave_axis, ushort follow_source, double ratio_numerator, double ratio_denominator, double acc, double dec, double s_time, ushort 
buffer_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_gear_in(ushort CardNo,ref ushort master_axis, ushort slave_axis,ref ushort follow_source,ref double ratio_numerator,ref double ratio_denominator,ref double acc,ref double dec,ref 
double s_time,ref ushort buffer_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_update_gear_scale(ushort CardNo, ushort slave_axis, double ratio_numerator, double ratio_denominator, double acc, double dec, double s_time);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_gear_in_pos(ushort CardNo, ushort master_axis, ushort slave_axis, ushort follow_source, double ratio_numerator, double ratio_denominator, double master_sync_pos, double slave_sync_pos, 
double master_start_dist, double velocity, double acc, double dec, double s_time, ushort buffer_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_gear_in_pos(ushort CardNo,ref ushort master_axis, ushort slave_axis, ref ushort follow_source,ref double ratio_numerator,ref double ratio_denominator,ref double master_sync_pos,ref 
double slave_sync_pos,ref double master_start_dist,ref double velocity,ref double acc,ref double dec,ref double s_time,ref ushort buffer_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_in_gear_state(ushort CardNo, ushort slave_axis,ref ushort in_gear);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_gear_aborted_state(ushort CardNo, ushort slave_axis, ref ushort aborted_state);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_gear_out(ushort CardNo, ushort slave_axis);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_set_config(ushort CardNo, short trace_cycle, short lost_handle, short trace_type, short trigger_object_index, short trigger_type, int mask, long condition);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_get_config(ushort CardNo,ref short trace_cycle,ref short lost_handle,ref short trace_type,ref short trigger_object_index,ref short trigger_type,ref int mask, ref long condition);




        /***********************************************************
             * ÅäÖÃ²É¼¯¶ÔÏó£¬Ò»´Î¿ÉÒÔÌí¼Ó500¸ö²É¼¯¶ÔÏó
             * data_type 	Êı¾İµÄÀàĞÍ£¬¼û²É¼¯¶ÔÏóËµÃ÷¡£
             * data_index 	Êı¾İµÄÖ÷Ë÷Òı£¬Èç¹ûÊÇ¸úÖáÏà¹Ø£¬ÔòÊÇÖáĞòºÅ£¬Èç¹ûÊÇIO£¬ÔòÊÇIOĞòºÅ£¬Èç´ËÀàÍÆ
             * data_sub_index Êı¾İµÄ×ÓË÷Òı£¬Èç¹ûÊÇ°´×é²É¼¯IO£¬Ôò±íÊ¾IO½áÊøµÄĞòºÅ¡£
             * data_bytes 	¶ÔÏó×Ö½ÚÊı£¬ÏÖÓĞ²É¼¯ÀàĞÍ»á×Ô¶¯Æ¥Åä£¬¹Ì¶¨Îª0£¬Ô¤ÁôºóĞøÀ©Õ¹¹¦ÄÜ
        **********************************************************/
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_reset_config_object(ushort CardNo );
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_add_config_object(ushort CardNo, short data_type, short data_index, short data_sub_index, short slave_id, short data_bytes);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_get_config_object(ushort CardNo, short object_index,ref short data_type,ref short data_index,ref short data_sub_index,ref short slave_id,ref short data_bytes);



        //Æô¶¯trace
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_data_start(ushort CardNo);

        //Í£Ö¹trace
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_data_stop(ushort CardNo);

        //¸´Î»trace£¬Í£Ö¹²É¼¯µÄÊ±ºò²ÅÄÜµ÷ÓÃ£¬»áÇå³ıÒÑ²É¼¯µ½µÄÊı¾İÒÔ¼°Òç³ö±êÖ¾Î»
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_data_reset(ushort CardNo);

        //traceÊÇ·ñÒÑ¾­Æô¶¯
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_get_flag(ushort CardNo,ref short start_flag,ref short triggered_flag,ref short lost_flag);


        /***********************************************************
           *¶ÁÈ¡²É¼¯×´Ì¬
           * valid_num 	ÒÑ²É¼¯µ«Î´±»¶ÁÈ¡µÄÊı¾İ¸öÊı
           * free_num 	Ê£Óà¿ÉÓÃÓÚ±£´æ²É¼¯Êı¾İµÄ¸öÊı
           * object_total_bytes   ²É¼¯¶ÔÏó×Ü×Ö½ÚÊı
           * object_total_num 	²É¼¯¶ÔÏó×Ü¸öÊı
       **********************************************************/
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_get_state(ushort CardNo,ref int valid_num,ref int free_num,ref int object_total_bytes,ref int object_total_num);

        /***********************************************************
         *¶ÁÈ¡²É¼¯Êı¾İ
         * bufsize 	Êı¾İ»º³åÇø×Ö½ÚÊı
         * data[1024] 	Êı¾İ»º³åÇø£¬
         * byte_size   ¶ÁÈ¡µÄÊı¾İµÄ×Ö½ÚÊı
         **********************************************************/

        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_get_data(ushort CardNo, int bufsize, string data,ref int byte_size);


        //trace¸´Î»Òç³öĞÅºÅ£¬Ö»ÊÇ¸´Î»±êÖ¾Î»
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_reset_lost_flag(ushort CardNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_message_buffer_enable(ushort CardNo, ushort index, UInt32 buffer_size, byte console_enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_message_buffer_disable(ushort CardNo, ushort index);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_message_buffer_get_data(ushort CardNo, ushort index, long bufsize, ref byte data, ref UInt32 pbufsize);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_t_pmove_extern_softstart(ushort CardNo, ushort axis, double MidPos, double TargetPos, double start_Vel, double Max_Vel, double stop_Vel, UInt32 delay_ms, double Max_Vel2, double 
stop_vel2, double acc_time, double dec_time, ushort posi_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_t_pmove_extern_softstart_unit(ushort CardNo, ushort axis, double MidPos, double TargetPos, double start_Vel, double Max_Vel, double stop_Vel, UInt32 delay_ms, double Max_Vel2, double 
stop_vel2, double acc_time, double dec_time, ushort posi_mode);





        //PVT_continuous½Ó¿Ú.
        //ÏÂ·¢PVT continuous¸÷½ÚµãÊı¾İ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pvt_table_continuous(ushort CardNo, ushort axis, UInt32 count, double[] pos, double[] vel, double[] percent, double[] vel_max, double[] acc, double[] dec);
        //¸ù¾İ´«ÈëµÄ²ÎÊı£¬»ñÈ¡¸÷¸öÎ»ÖÃ½ÚµãµÄÊ±¼ä
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pvt_continuous_calculate(ushort CardNo, ushort axis, double[] time);
        //¿ªÆôPVT continuous ÔË¶¯
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pvt_continuous_start(ushort CardNo, ushort axis_num, double[] axis_list, double[] start_delay_time);



        //·´À¡Î»ÖÃÎó²îÔÊĞí·¶Î§ÉèÖÃ

        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_set_allow_error(ushort CardNo, ushort group, double allow_error);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_get_allow_error(ushort CardNo, ushort group,ref double allow_error);


        //ÈıµãÔ²»¡
        [DllImport("LTDMC.dll")]
        public static extern short dmc_arc_move_3points_multicoor(ushort CardNo, ushort Crd, ushort[] AxisList, double[] Target_Pos, double[] Mid_Pos, long Circle, ushort posi_mode);




        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_delay_outbit_to_start_path(ushort CardNo, ushort Crd, ushort bitno, ushort on_off, double delay_value, ushort delay_mode, double ReverseTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_delay_outbit_to_stop_path(ushort CardNo, ushort Crd, ushort bitno, ushort on_off, double delay_time, double ReverseTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_ahead_outbit_to_stop_path(ushort CardNo, ushort Crd, ushort bitno, ushort on_off, double ahead_value, ushort ahead_mode, double ReverseTime);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_line_section_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] pPosList, double Section_Dist, ushort Bitno, ushort On_Off, ushort Io_Mode, double 
Time_Dist_Value, double ReverseTime, ushort posi_mode, ushort mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_arc_move_center_section_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Cen_Pos, ushort Arc_Dir, ushort Circle, double 
Section_Dist, ushort Bitno, ushort On_Off, ushort Io_Mode, double Time_Dist_Value, double ReverseTime, ushort posi_mode, ushort mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_arc_move_radius_section_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double Arc_Radius, ushort Arc_Dir, ushort Circle, double 
Section_Dist, ushort Bitno, ushort On_Off, ushort Io_Mode, double Time_Dist_Value, double ReverseTime, ushort posi_mode, ushort mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_arc_move_3points_section_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Mid_Pos, ushort Circle, double Section_Dist, ushort Bitno
, ushort On_Off, ushort Io_Mode, double Time_Dist_Value, double ReverseTime, ushort posi_mode, ushort mark);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_pci_int(ushort CardNo);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_ad_monitor_config(ushort CardNo, ushort Crd, ushort CANid, ushort channel, ushort ADEn, double ADValDown, double ADValUp);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_ad_monitor_config(ushort CardNo, ushort Crd,ref ushort CANid,ref ushort channel,ref ushort ADEn,ref double ADValDown,ref double ADValUp);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_ad_monitor_result(ushort CardNo, ushort Crd, ref ushort ch, ref double ADRet, ref ushort num, ref double pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_clear_ad_monitor_result(ushort CardNo, ushort Crd);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_update_target_position_extern_unit(ushort CardNo, ushort axis, double mid_pos, double aim_pos, double vel, ushort posi_mode);



        //Ö¸¶¨Öá×ö¶¨³¤Î»ÒÆÔË¶¯ Í¬Ê±·¢ËÍËÙ¶ÈºÍSÊ±¼ä(µ±Á¿)
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pmove_extern_unit(ushort CardNo, ushort axis, double dist, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double stop_Vel, double s_para, ushort posi_mode);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_pmove_extern_acc_unit(ushort CardNo, ushort axis, double dist, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double stop_Vel, double s_para, ushort posi_mode);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_set_enable(ushort CardNo, ushort Crd, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_get_enable(ushort CardNo, ushort Crd , ref ushort enable);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_get_state(ushort CardNo, ushort Crd, ref long remained_space);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_clear_points(ushort CardNo, ushort Crd);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_set_config_params(ushort CardNo, ushort Crd, ushort Bitno, ushort On_Off, ushort Io_Mode, double Time_Dist_Value, double ReverseTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_get_config_params(ushort CardNo, ushort Crd,ref ushort Bitno,ref ushort On_Off,ref ushort Io_Mode,ref double Time_Dist_Value,ref double ReverseTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_line_add_cmp_fifo_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] cmp_pos, ushort num, ushort posi_mode, long mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_arc_move_3points_add_cmp_fifo_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Mid_Pos, ushort Circle, double[] cmp_pos, ushort num
, ushort posi_mode, long mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_arc_move_center_add_cmp_fifo_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Cen_Pos, ushort Arc_Dir, ushort Circle, double[] 
cmp_pos, ushort num, ushort posi_mode, long mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_arc_move_radius_add_cmp_fifo_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double Arc_Radius, ushort Arc_Dir, ushort Circle, double[] 
cmp_pos, ushort num, ushort posi_mode, long mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_get_total_point(ushort CardNo, ushort Crd, ref long total_point);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_get_remain_point(ushort CardNo, ushort Crd, ref long remain_point);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_get_trig_point(ushort CardNo, ushort Crd, ref long trig_point);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_get_force_trig_point(ushort CardNo, ushort Crd, ref long force_trig_point);




        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_wait_node_input_delay_to_start(ushort CardNo, ushort Crd, ushort node_ID, ushort bitno, ushort on_off, double delay_value, ushort delay_mode, double TimeOut);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_wait_node_input_ahead_to_stop(ushort CardNo, ushort Crd, ushort node_ID, ushort bitno, ushort on_off, double delay_value, ushort ahead_mode, double TimeOut);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_delay_node_outbit_to_start(ushort CardNo, ushort Crd, ushort node_ID, ushort bitno, ushort on_off, double delay_value, ushort delay_mode, double ReverseTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_delay_node_outbit_to_stop(ushort CardNo, ushort Crd, ushort node_ID, ushort bitno, ushort on_off, double delay_time, double ReverseTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_ahead_node_outbit_to_stop(ushort CardNo, ushort Crd, ushort node_ID, ushort bitno, ushort on_off, double ahead_value, ushort ahead_mode, double ReverseTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_write_node_outbit(ushort CardNo, ushort Crd, ushort node_ID, ushort bitno, ushort on_off, double ReverseTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_clear_node_io_action(ushort CardNo, ushort Crd, ushort node_ID, UInt32 Io_Mask);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_connect_type(ushort CardNo,ref ushort ConnectType);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_board_init_eth(ushort CardNo,string IpAddr);


        //¼õËÙÍ£Ö¹¾àÀë
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_dec_stop_dist_unit(ushort CardNo, ushort axis, double dist);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_dec_stop_dist_unit(ushort CardNo, ushort axis,ref double dist);



        //×î´óËÙ¶ÈÏŞÖÆÉèÖÃ(Âö³åµ±Á¿)
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_profile_limit_unit(ushort CardNo, ushort axis, double Limit_Max_Vel, double Limit_Max_Acc, double EvenTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_profile_limit_unit(ushort CardNo, ushort axis,ref double Limit_Max_Vel,ref double Limit_Max_Acc,ref double EvenTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_vector_profile_limit_unit(ushort CardNo, ushort axis, double Limit_Max_Vel, double Limit_Max_Acc, double EvenTime);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_vector_profile_limit_unit(ushort CardNo, ushort axis, ref double Limit_Max_Vel, ref double Limit_Max_Acc, ref double EvenTime);



        //ÆôÓÃµ¥ÖáÏŞËÙ¹¦ÄÜ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_vector_profile_limit_by_axis(ushort CardNo, ushort Crd, ushort Enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_vector_profile_limit_by_axis(ushort CardNo, ushort Crd,ref ushort Enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_follow_line_enable(ushort CardNo, ushort Crd, ref ushort enable_flag);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_counter_reverse(ushort CardNo, ushort axis, ushort reverse);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_counter_reverse(ushort CardNo, ushort axis,ref ushort reverse);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_extra_counter_reverse(ushort CardNo, ushort axis, ushort reverse);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_extra_counter_reverse(ushort CardNo, ushort axis, ref ushort reverse);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_stop_axis(ushort CardNo, ushort Crd, ushort axis, double dec, int imark);


        //¶ÁÈ¡²å²¹³¤¶È

        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_vector_length_unit(ushort CardNo, ushort Crd,ref double total_length,ref double left_length);




        /*********************************************************************************************************
        ¼òÒ×µç×ÓÍ¹ÂÖÔË¶¯
        *********************************************************************************************************/
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_table_unit(ushort CardNo, ushort MasterAxisNo, ushort SlaveAxisNo, UInt32 Count, double[] pMasterPos, double[] pSlavePos, ushort SrcMode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_move(ushort CardNo, ushort axis);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_move_cycle(ushort CardNo, ushort axis);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_fairing_enable(ushort CardNo, ushort Crd, ushort enable, double fairing_length);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_get_fairing_enable(ushort CardNo, ushort Crd, ref ushort enable,ref double fairing_length);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_eth_timeout(int timems);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_extra_encoder_extern(ushort CardNo, ushort channel, int pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_extra_encoder_extern(ushort CardNo, ushort channel,ref int pos);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_smooth_contour_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, ushort point_num, double[] x, double[] y, double[] z, double vel_coef, double eps, long mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_conti_smooth_contour_curve(ushort point_num, double[] x, double[] y, double[] z, double eps,ref double curve_x,ref double curve_y,ref double curve_z,ref double length);




        //µç×ÓÍ¹ÂÖ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_in(ushort CardNo, ushort slave_axis, ushort master_axis, ushort execute, ushort conti_update, ushort cam_table, ushort periodic, ushort master_abs, ushort slave_abs, double 
master_offset, double slave_offset, double master_scaling, double slave_scaling, double master_start_dist, double master_sync_pos, double active_pos, ushort active_mode, ushort start_mode, double velocity, double acc
, double dec, double jerk, ushort master_source, ushort buffer_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_cam_in_status(ushort CardNo, ushort slave_axis,ref ushort in_sync,ref ushort end_of_profile,ref ushort busy,ref ushort active,ref ushort cmd_aborted,ref ushort error,ref ushort 
error_id);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_out(ushort CardNo, ushort slave_axis, ushort execute);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_cam_out_status(ushort CardNo, ushort slave_axis,ref ushort done,ref ushort busy,ref ushort cmd_aborted,ref ushort error,ref UInt32 error_id);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_read_points(ushort CardNo, ushort execute, ushort cam_table, ushort cam_chg_point, UInt32 cam_point_num,  ushort[] done, ushort[] busy, ushort[] error, UInt32[]  error_id,ref double
 master_pos,ref double slave_pos,ref double slave_vel,ref double slave_acc);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_write_points(ushort CardNo, ushort execute, ushort cam_table, UInt32 cam_point_num, double master_pos, double slave_pos, double slave_vel, double slave_acc, ushort[] done, ushort[] 
busy, ushort[] error, UInt32[] error_id);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_set(ushort CardNo, ushort execute, ushort cam_table, ushort[] done, ushort[] busy, ushort[] error, UInt32[] error_id);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_read_tappet_status(ushort CardNo, ushort execute, ushort cam_table, UInt32 tappet_num1, UInt32 tappet_num2, UInt32 tappet_num3, UInt32 tappet_num4, UInt32 tappet_num5, UInt32 
tappet_num6, UInt32 tappet_num7, UInt32 tappet_num8, ref ushort valid,ref ushort busy,ref ushort error,ref UInt32 error_id,ref ushort status1,ref ushort status2, ref ushort status3, ref ushort status4, ref ushort 
status5, ref ushort status6, ref ushort status7, ref ushort status8);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_read_tappet_value(ushort CardNo, ushort execute, ushort cam_table, UInt32 tappet_num, ref ushort valid, ref ushort busy, ref ushort error, ref UInt32 error_id,ref double master_pos,
 ref ushort positive_mode,ref ushort negative_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_write_tappet_value(ushort CardNo, ushort execute, ushort cam_table, UInt32 tappet_num, double master_pos, ushort positive_mode, ushort negative_mode, ushort[] done, ushort[] busy, 
ushort[] error, UInt32[] error_id);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_add_tappet(ushort CardNo, ushort execute, ushort cam_table, double master_pos, ushort positive_mode, ushort negative_mode, ushort[] done, ushort[] busy, ushort[] error, UInt32[] 
error_id, UInt32[] tappet_num);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_delete_tappet(ushort CardNo, ushort execute, ushort cam_table, ushort[] done, ushort[] busy, ushort[] error, UInt32[] error_id);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_temp_delete(ushort CardNo, ushort group, ushort addr, ushort num, ushort delete_mode);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_wait_mode(ushort CardNo, ushort Crd, ushort wait_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_get_wait_mode(ushort CardNo, ushort Crd,ref ushort wait_mode);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_peak_config(ushort CardNo, ushort axis, ushort enable, double u_time);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_peak_config(ushort CardNo, ushort axis,ref ushort enable,ref double u_time);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_err_band(ushort CardNo, ushort axis, double err_band, ushort set_cycle);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_err_band(ushort CardNo, ushort axis,ref double err_band,ref ushort set_cycle);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_err_band_unit(ushort CardNo, ushort axis, double err_band, ushort set_cycle);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_err_band_unit(ushort CardNo, ushort axis,ref double err_band,ref ushort set_cycle);

    



        //ĞÂÔöÕë¶ÔÄ£¿é¸ßËÙ±È½ÏÖ¸Áî
        [DllImport("LTDMC.dll")]
        public static extern short nmc_hcmp_set_mode(ushort CardNo, ushort PortNum, ushort nodenum, ushort hcmp, ushort cmp_mode);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_hcmp_get_mode(ushort CardNo, ushort PortNum, ushort nodenum, ushort hcmp,ref ushort cmp_mode);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_hcmp_set_config(ushort CardNo, ushort PortNum, ushort nodenum, ushort hcmp, ushort channel, ushort cmp_source, ushort cmp_logic, long time);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_hcmp_get_config(ushort CardNo, ushort PortNum, ushort nodenum, ushort hcmp,ref ushort channel,ref ushort cmp_source,ref ushort cmp_logic,ref long time);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_hcmp_clear_points(ushort CardNo, ushort PortNum, ushort nodenum, ushort hcmp, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_hcmp_add_point(ushort CardNo, ushort PortNum, ushort nodenum, ushort hcmp, long cmp_pos);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_hcmp_set_liner(ushort CardNo, ushort PortNum, ushort nodenum, ushort hcmp, long Increment, long Count);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_hcmp_get_liner(ushort CardNo, ushort PortNum, ushort nodenum, ushort hcmp,ref long Increment,ref long Count);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_hcmp_get_current_state(ushort CardNo, ushort PortNum, ushort nodenum, ushort hcmp,ref long remained_points,ref long current_point,ref long runned_points);





        //ÑùÌõÇúÏßÏà¹Ø
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_follow_trajectory_displacement(ushort CardNo, ushort crd, ushort num, ushort[] Axis_list);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_follow_trajectory_displacement(ushort CardNo, ushort crd,ref ushort num, ushort[] Axis_list);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_tool_length_compensation_param(ushort CardNo, ushort axis, double length);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_tool_length_compensation_param(ushort CardNo, ushort axis,ref double length);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_tool_length_compensation_enable(ushort CardNo, ushort axis, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_tool_length_compensation_enable(ushort CardNo, ushort axis,ref ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_normal_direction_control(ushort CardNo, ushort crd, ushort axis, ushort mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_normal_direction_control(ushort CardNo, ushort crd,ref ushort axis,ref ushort mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_gap_cmp_param(ushort CardNo, ushort crd, ushort pin, ushort logic, ushort mode, ushort auxi_axis, ushort source, long rev_time, double[] gap);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_gap_cmp_param(ushort CardNo, ushort crd,ref ushort pin,ref ushort logic,ref ushort mode,ref ushort auxi_axis,ref ushort source,ref long rev_time, double[] gap);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_gap_cmp_enable(ushort CardNo, ushort crd, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_gap_cmp_enable(ushort CardNo, ushort crd,ref ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_normal_direction_control_enable(ushort CardNo, ushort crd, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_normal_direction_control_enable(ushort CardNo, ushort crd,ref ushort enable);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_mc_gear_in(ushort CardNo, ushort slave_axis, ushort master_axis, ushort execute, ushort conti_update, ushort master_source, double ratio_numerator, double ratio_denominator, double acc,
 double dec, double jerk, ushort buffer_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_mc_gear_in(ushort CardNo, ushort slave_axis,ref ushort master_axis, ref ushort execute, ref ushort conti_update, ref ushort master_source, ref double ratio_numerator, ref double 
ratio_denominator, ref double acc, ref double dec, ref double jerk, ref ushort buffer_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_mc_gearin_status(ushort CardNo, ushort slave_axis,ref ushort in_gear,ref ushort busy,ref ushort active,ref ushort cmd_aborted,ref ushort error, ref UInt32 error_id);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_mc_gear_out(ushort CardNo, ushort slave_axis, ushort[] execute);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_mc_gear_out_status(ushort CardNo, ushort slave_axis,ref ushort done,ref ushort busy,ref ushort cmd_aborted,ref ushort error, ref UInt32 error_id);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_mc_combine_axes(ushort CardNo, ushort slave_axis, ushort master_axis1, ushort master_axis2, ushort execute, ushort conti_update, ushort master_source1, ushort master_source2, ushort 
combine_mode, double ratio_numerator1, double ratio_denominator1, double ratio_numerator2, double ratio_denominator2, double acc, double dec, double jerk, ushort buffer_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_mc_combine_axes(ushort CardNo, ushort slave_axis, ref ushort master_axis1, ref ushort master_axis2, ref ushort execute, ref ushort conti_update, ref ushort master_source1, ref 
ushort master_source2, ref ushort combine_mode, ref double ratio_numerator1, ref double ratio_denominator1, ref double ratio_numerator2, ref double ratio_denominator2, ref double acc, ref double dec, ref double jerk,
 ref ushort buffer_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_mc_combine_axes_status(ushort CardNo, ushort slave_axis,ref ushort in_sync,ref ushort busy,ref ushort active,ref ushort cmd_aborted,ref ushort error,ref UInt32 error_id);




        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_space_collision_zone_param(ushort CardNo, ushort axis_num, ushort[] axis_list, ushort zone_num, double[] neg_limit, double[] pos_limit, ushort stop_mode, ushort pos_source);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_space_collision_zone_param(ushort CardNo,ref ushort axis_num, ushort[] axis_list, ref ushort zone_num, double[] neg_limit, double[] pos_limit, ref ushort stop_mode, ref ushort 
pos_source);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_space_collision_zone_enable(ushort CardNo, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_space_collision_zone_enable(ushort CardNo,ref ushort enable);



        //ÅúÁ¿¶ÁÈ¡ ²»´øµ±Á¿20201016
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_position_extern(ushort CardNo, ushort axis,ref double pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_encoder_extern(ushort CardNo, ushort axis, ref double pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_current_speed_extern(ushort CardNo, ushort axis, ref double current_speed);






        //»Ø¶ÁpmoveÔË¶¯¹æ»®×ÜÊ±¼ä¼°Ê£ÓàÊ±¼ä
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_plan_time_info(ushort CardNo, ushort axis,ref double sum_time,ref double remain_time);
        //ÉèÖÃ²å²¹µ¥Öá×î´óÔÊĞíËÙ¶ÈÖµ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_max_interpo_speed(ushort CardNo, ushort axis, double max_speed);
        //»Ø¶Á²å²¹µ¥Öá×î´óÔÊĞíËÙ¶ÈÖµ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_max_interpo_speed(ushort CardNo, ushort axis,ref double max_speed);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_max_interpo_speed_enable(ushort CardNo, ushort axis, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_max_interpo_speed_enable(ushort CardNo, ushort axis,ref ushort enable);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_diagnosis_log_enable(ushort CardNo, ushort Crd, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_diagnosis_log_enable(ushort CardNo, ushort Crd,ref ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_diagnosis_log_data(ushort CardNo, ushort Crd);

        [DllImport("LTDMC.dll")]
        public static extern short nmc_reverse_outbit(ushort CardNo, ushort Channel, ushort NoteID, ushort IoBit, double reverse_time);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sine_oscillate(ushort CardNo, ushort Axis, double Amplitude, double Frequency);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sine_oscillate_stop(ushort CardNo, ushort Axis);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_apf_rotary_cut_init(ushort CardNo, UInt32 rotary_cut_id, ushort execute, double rotary_axis_radius, UInt32 rotary_axis_knife_num, double feed_axis_radius, double cutlength, double 
sync_start_pos, double sync_stop_pos, double rot_start_pos, double fed_stop_pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_apf_rotary_cut_init(ushort CardNo, UInt32 rotary_cut_id,ref ushort execute,ref double rotary_axis_radius,ref UInt32 rotary_axis_knife_num,ref double feed_axis_radius,ref double 
cutlength,ref double sync_start_pos,ref double sync_stop_pos,ref double rot_start_pos,ref double fed_stop_pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_apf_rotary_cut_init_status(ushort CardNo, UInt32 rotary_cut_id, ref ushort done,ref ushort busy,ref ushort error,ref UInt32 error_id);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_apf_rotary_cut_in(ushort CardNo, UInt32 rotary_cut_id, ushort execute, ushort rotary_axis, ushort feed_axis);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_apf_rotary_cut_in_status(ushort CardNo, UInt32 rotary_cut_id, ref ushort done, ref ushort busy, ref ushort error, ref UInt32 error_id);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_apf_rotary_cut_in(ushort CardNo, UInt32 rotary_cut_id, ref ushort execute, ref ushort rotary_axis, ref ushort feed_axis);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_apf_rotary_cut_out(ushort CardNo, UInt32 rotary_cut_id, ushort execute, ushort rotary_axis);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_apf_rotary_cut_out_status(ushort CardNo, UInt32 rotary_cut_id, ref ushort done, ref ushort busy,ref ushort error, ref UInt32 error_id);




        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_clear_current_mark_mode(ushort CardNo, ushort Crd, ushort mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_get_clear_current_mark_mode(ushort CardNo, ushort Crd,ref ushort mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_clear_current_mark(ushort CardNo, ushort Crd);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_arc_translate_mode(ushort CardNo, ushort Crd, ushort mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_get_arc_translate_mode(ushort CardNo, ushort Crd,ref ushort mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_trace_set_source(ushort CardNo, ushort source);

        [DllImport("LTDMC.dll")]
        public static extern short nmc_sync_set_profile_unit(ushort CardNo, ushort AxisNum, ushort[] AxisList, double[] Min_Vel, double[] Max_Vel, double[] Tacc, double[] Tdec, double[] Stop_Vel);



        [DllImport("LTDMC.dll")]
        public static extern short nmc_write_rxpdo_extra_short(ushort CardNo, ushort PortNum, ushort address, ushort DataLen, ushort Value);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_rxpdo_extra_short(ushort CardNo, ushort PortNum, ushort address, ushort DataLen, ref ushort Value);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_read_txpdo_extra_short(ushort CardNo, ushort PortNum, ushort address, ushort DataLen, ref ushort Value);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_timeout(ushort CardNo, UInt32 timems);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_sync_pos_change_mode(ushort CardNo, ushort portno, ushort axis);




        [DllImport("LTDMC.dll")]
        public static extern short dmc_mc_gear_in_pos(ushort CardNo, ushort slave_axis, ushort master_axis, ushort execute, ushort conti_update, ushort master_source, double ratio_numerator, double ratio_denominator, double 
master_sync_pos, double slave_sync_pos, double master_start_dist, ushort buffer_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_mc_gear_in_pos(ushort CardNo, ushort slave_axis, ref ushort master_axis, ref ushort execute, ref ushort conti_update, ref ushort master_source, ref double ratio_numerator, ref 
double ratio_denominator, ref double master_sync_pos, ref double slave_sync_pos, ref double master_start_dist, ref ushort buffer_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_mc_gear_in_pos_status(ushort CardNo, ushort slave_axis, ref ushort start_sync, ref ushort in_sync, ref ushort busy, ref ushort active, ref ushort cmd_aborted, ref ushort error, ref 
UInt32 error_id);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_watchdog_trig_status(ushort CardNo,ref ushort status);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_reset_watchdog_trig_status(ushort CardNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_transarc_io_insert_mode(ushort CardNo, ushort Crd, ushort mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_get_transarc_io_insert_mode(ushort CardNo, ushort Crd,ref ushort mode);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_multi_axes_motion_sync_pmove_unit(ushort CardNo, ushort axis_num, ushort[] axis_list, double[] dist_list, double[] Min_Vel_list, double[] Max_Vel_list, double[] Tacc_list, double[] 
Tdec_list, double[] stop_Vel_list, double[] s_para_list, ushort[] posi_mode_list, ushort mode);




        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_ez_map_input(ushort CardNo, ushort axis, ushort enable, ushort mode, ushort index, ushort sub_index);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_ez_map_input(ushort CardNo, ushort axis,ref ushort enable,ref ushort mode,ref ushort index,ref ushort sub_index);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_etc_el_stop_mode(ushort CardNo, ushort axis, ushort el_control_mode, double diff_pos, UInt32 filter);




        [DllImport("LTDMC.dll")]
        public static extern short dmc_circle_move_center_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Cen_Pos, ushort Arc_Dir, long Circle, ushort posi_mode);     
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_circle_move_center_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Cen_Pos, ushort Arc_Dir, long Circle, ushort posi_mode, long 
mark);    
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_acuate_angle_config_params(ushort CardNo, ushort Crd, double acuate_angle, double angle_trans_speed, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_acuate_angle_config_params(ushort CardNo, ushort Crd,ref double acuate_angle,ref double angle_trans_speed,ref ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axes_link_params(ushort CardNo, ushort master, ushort slave);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axes_link_params(ushort CardNo, ushort master,ref ushort slave);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_remove_axes_link_params(ushort CardNo, ushort master);





        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_alm_mode_ex(ushort CardNo, ushort axis, ushort enable, ushort alm_logic, ushort alm_action, ushort alm_all);//ĞÂÔöÅäÖÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_alm_mode_ex(ushort CardNo, ushort axis,ref ushort enable,ref ushort alm_logic,ref ushort alm_action,ref ushort alm_all);//ĞÂÔöÅäÖÃ





        //¶ÁÈ¡±àÂëÆ÷·½Ïò

        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_encoder_dir(ushort CardNo, ushort axis,ref ushort dir);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_download_configfile_ex(ushort CardNo,string FileName);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_s_profile_config(ushort CardNo, ushort axis, double acc_s_time, double dec_s_time);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_s_profile_config(ushort CardNo, ushort axis,ref double acc_s_time,ref double dec_s_time);

        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_slave_state(ushort CardNo, ushort SlaveId, ushort SlaveState);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_slave_state(ushort CardNo, ushort SlaveId,ref ushort SlaveState);







        [DllImport("LTDMC.dll")]
        public static extern short dmc_sync_pmove_unit(ushort CardNo, ushort axis_num, ushort[] axis_list, double[] dist_list, ushort[] posi_mode_list, ushort mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_abnormal_mode(ushort CardNo, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_abnormal_mode(ushort CardNo,ref ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_clear_axis_abnormal_state(ushort CardNo, ushort axis, ushort count);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_coordinate_abnormal_mode(ushort CardNo, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_coordinate_abnormal_mode(ushort CardNo,ref ushort enable);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_clear_crd_abnormal_state(ushort CardNo, ushort Crd, ushort count);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_coordinate_remainspace_mode(ushort CardNo, ushort Crd, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_coordinate_remainspace_mode(ushort CardNo, ushort Crd,ref ushort enable);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_hcmp_add_linear_unit(ushort CardNo, ushort hcmp, int count, struct_hs_cmp_info[] cmp_str); 

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_handwheel_encoder_filter_frequancy(ushort CardNo, ushort axis, double frequancy);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_handwheel_encoder_filter_frequancy(ushort CardNo, ushort axis,ref double frequancy);


        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_slave_alias(ushort CardNo, ushort portnum, ushort auto_address, ushort alias_address);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_slave_alias(ushort CardNo, ushort portnum, ushort auto_address,ref ushort alias_address);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_pwm_first_pulse_mode(ushort CardNo, ushort pwm_no, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pwm_first_pulse_mode(ushort CardNo, ushort pwm_no,ref ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_pwm_first_pulse_duty(ushort CardNo, ushort pwm_no, double duty);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pwm_first_pulse_duty(ushort CardNo, ushort pwm_no,ref double duty);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_set_hcmp2d_pos_ratio(ushort CardNo, ushort Crd, ushort hcmp2d, double xpos_ratio, double ypos_ratio);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_get_hcmp2d_pos_ratio(ushort CardNo, ushort Crd, ushort hcmp2d,ref double xpos_ratio,ref double ypos_ratio);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_leadscrew_comp_datasheet_enable(ushort CardNo, ushort axis, ushort enable, int point_num);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_leadscrew_comp_datasheet_enable(ushort CardNo, ushort axis,ref ushort enable,ref int point_num);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_pos_calibrate_config(ushort CardNo, ushort axis, ushort settle_time, double err_band, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pos_calibrate_config(ushort CardNo, ushort axis,ref ushort settle_time,ref double err_band,ref ushort enable);



        //ÎÄ¼şµ÷ÓÃ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_userlib_loadlibrary(ushort CardNo,string  pLibname);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_userlib_set_parameter(ushort CardNo, int type, string pParameter,int length);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_userlib_get_parameter(ushort CardNo, int type, string pParameter, int length);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_userlib_imd_stop(ushort CardNo, ushort axis);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_get_fpga_receive_point(ushort CardNo, ushort cmp_no,ref long receive_point);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cmp_fifo_check_fpga_clear_status(ushort CardNo, ushort cmp_no,ref ushort clr_status,ref long clr_point);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_modify_slaveid(ushort CardNo, ushort index, ushort subindex, ushort newindex,string FileName);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_home_finish_map(ushort CardNo, ushort axis, ushort enable, ushort mode, ushort index, ushort sub_index, ushort bit_index, ushort bit_logic);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_home_finish_map(ushort CardNo, ushort axis,ref ushort enable,ref ushort mode,ref ushort index,ref ushort sub_index,ref ushort bit_index,ref ushort bit_logic);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_config_error_info(ushort CardNo,ref int axis,ref int liner,ref int type,ref int errorcode);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_t_pmove_extern_dectime(ushort CardNo, ushort axis, UInt32 dec_time);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_t_pmove_extern_dectime(ushort CardNo, ushort axis,ref UInt32 dec_time);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_trajectory_splicing_error(ushort CardNo, ushort crd, double error);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_trajectory_splicing_error(ushort CardNo, ushort crd,ref double error);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_sine_oscillate_set_cycle_num(ushort CardNo, ushort Axis, UInt32 cycle_num);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_sine_oscillate_get_cycle_num(ushort CardNo, ushort Axis,ref UInt32 cycle_num);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_wait_input_action(ushort CardNo, ushort Crd, ushort bitno, ushort on_off, double TimeOut, ushort action, long mark);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_line_change_pos_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] TargetPos);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_arc_move_angle_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Cen_Pos, double Angle, double[] Target_Pos, ushort posi_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_arc_move_center_angle_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Cen_Pos, double Angle, ushort Arc_Dir, long Circle, ushort 
posi_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_line_change_pos_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] TargetPos, int mark);



        [DllImport("LTDMC.dll")]
        public static extern short nmc_sync_pmove_extern_unit(ushort CardNo, ushort AxisNum, ushort[] AxisList, double[] Dist, double[] Max_Vel, ushort Posimode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_pvt_get_run_index(ushort CardNo, ushort axis, ref UInt32 index);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_firmware_auto_update();
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_blend_distance(ushort CardNo, ushort Crd, ushort Enable, double BlendDistance);


        //////////////////////////////////////////////////////////////////////////2022.08.11ĞÂÔö

        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_dc_mode(ushort CardNo, ushort PortNo, ushort mode);

        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_dc_mode(ushort CardNo, ushort PortNo,ref ushort mode);




        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_gear_follow_ratio(ushort CardNo, ushort axis, double ratio);//Ë«ZÖá
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_gear_follow_ratio(ushort CardNo, ushort axis, ref double ratio);


        //LTC·´ÏàÊä³ö¹¦ÄÜ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_ltc_set_outbit(ushort CardNo, ushort latch, ushort enable, ushort bitno, ushort logic, double delaytime_s, double outtime_s);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_ltc_get_outbit(ushort CardNo, ushort latch,ref ushort enable,ref ushort bitno, ref ushort logic, ref double delaytime_s, ref double outtime_s);




        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_idle_crd_index(ushort CardNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_position_virtual(ushort CardNo, ushort axis,ref double pos);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_check_encoder_done(ushort CardNo, ushort axis,ref ushort state,ref double EncoderPos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_check_target_encoder(ushort CardNo, ushort axis, ushort TargetCheckEnable, double TargetError, double TargetCheckTime_s);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_check_target_encoder(ushort CardNo, ushort axis, ref ushort TargetCheckEnable, ref double TargetError, ref double TargetCheckTime_s);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_check_inp_encoder(ushort CardNo, ushort axis, ushort InpCheckEnable, double InpError, double InpCheckTime_s);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_check_inp_encoder(ushort CardNo, ushort axis,ref ushort InpCheckEnable, ref double InpError, ref double InpCheckTime_s);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_connect_to_encoder(ushort CardNo, ushort axis, ushort enable, double error);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_connect_to_encoder(ushort CardNo, ushort axis,ref ushort enable,ref double error);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_robot_config(ushort CardNo, ushort Crd, short robot_type, short elbow, short joint_num, short[] joint_list, double[] rx, double[] tx, double[] rz, double[] tz);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_robot_config(ushort CardNo, ushort Crd,ref short robot_type, ref short elbow, ref short joint_num, short[] joint_list, double[] rx, double[] tx, double[] rz, double[] tz);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_robot_enable(ushort CardNo, ushort Crd, short user_crd, short tool_crd, short enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_robot_ptp_move(ushort CardNo, ushort Crd, short joint_num, short[] joint_list, double[] joint_pos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_robot_sts(ushort CardNo, ushort Crd,ref short complete,ref short user_crd,ref short tool_crd,ref short enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_robot_pos(ushort CardNo, ushort Crd,ref double pos);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_robot_kinematics_calib(ushort CardNo, ushort Crd, double[] delta_rx, double[] delta_tx, double[] delta_rz, double[] delta_tz);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_robot_kinematics_calib(ushort CardNo, ushort Crd, double[] delta_rx, double[] delta_tx, double[] delta_rz, double[] delta_tz);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_robot_kinematics_calib(ushort CardNo, ushort Crd, double[] ja, double[] jb, double[] jc, double[] jd, double[] je, double[] jf, double[] jg, double[] jh, double[] ji, double[] delta_x, 
double[] delta_y, double[] delta_z);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_robot_user_coordinate(ushort CardNo, ushort Crd, short user_crd, short complete, double[] mat);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_robot_user_coordinate(ushort CardNo, ushort Crd, short user_crd,ref short complete, double[] mat);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_robot_user_coordinate(ushort CardNo, ushort Crd, short user_crd, double[] p0, double[] px, double[] py);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_robot_tool_coordinate(ushort CardNo, ushort Crd, short tool_crd, short complete, double[] mat);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_robot_tool_coordinate(ushort CardNo, ushort Crd, short tool_crd,ref short complete, double[] mat);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_robot_tool_coordinate(ushort CardNo, ushort Crd, short tool_crd, double[] p1, double[] p2, double[] p3, double[] p4, double[] p5, double[] p6, double[] p0, double[] px, double[] py);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_robot_workspace_detect(ushort CardNo, ushort Crd, double[] pos);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_wait_flag(ushort CardNo, ushort Crd, int mark, ushort wait_flag);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_get_wait_flag(ushort CardNo, ushort Crd,ref int mark,ref ushort wait_flag);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_arc_blend_enable(ushort CardNo, ushort Crd, ushort Enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_get_arc_blend_enable(ushort CardNo, ushort Crd,ref ushort Enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_t_pmove_extern_unit_ex(ushort CardNo, ushort axis, double MidPos, double TargetPos, double Min_Vel, double Max_Vel, double stop_Vel, double acc, double dec, ushort posi_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_check_success_pulse_ex(ushort CardNo, ushort axis, int delay_ms);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_check_success_encoder_ex(ushort CardNo, ushort axis, int delay_ms);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_write_points_packet(ushort CardNo, ushort cam_table_id, ushort cam_point_num, double s_range_up, double s_range_dn, double[] master_pos, double[] slave_pos, double[] slave_vel, 
double[] slave_acc, double[] slave_jerk, ushort[] type);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_cam_read_points_packet(ushort CardNo, ushort cam_table_id,ref ushort cam_point_num,ref double s_range_up,ref double s_range_dn, double[] master_pos, double[] slave_pos, double[] 
slave_vel, double[] slave_acc, double[] slave_jerk, ushort[] type);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_pwm_output_extern(ushort CardNo, ushort pwm, ushort enable, double width_us, double frequency, int number);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_spline_pmove(ushort CardNo, ushort axis, double pos, double vs, double vm, double ve, double as1, double ae, double rmd_as, double rmd_ae, int num_ts, int num_tm, int num_te, double 
cur_as, double cur_ae, ushort posi_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_plan_mode(ushort CardNo, ushort axis, ushort mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_plan_mode(ushort CardNo, ushort axis,ref ushort mode);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_emg_lock(ushort CardNo, ushort enable, ushort bit_no, ushort level, Int32 out_mark, Int32 out_level);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_emg_lock(ushort CardNo,ref ushort enable,ref ushort bit_no, ref ushort level,ref Int32 out_mark, ref Int32 out_level);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_emg_unlock(ushort CardNo);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_emg_lock_status(ushort CardNo,ref ushort lock_status,ref ushort lock_type);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_vector_profile_extern(ushort CardNo, ushort Crd, double Min_Vel, double Max_Vel, double Acc, double Dec, double Ajerk, double Djerk, double stop_vel);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_vector_profile_extern(ushort CardNo, ushort Crd,ref double Min_Vel,ref double Max_Vel,ref double Acc,ref double Dec,ref double Ajerk,ref double Djerk,ref double stop_vel);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_vector_plan_mode(ushort CardNo, ushort Crd, ushort mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_vector_plan_mode(ushort CardNo, ushort Crd,ref ushort mode);



        [DllImport("LTDMC.dll")]
        public static extern short nmc_set_data_offset_time(ushort CardNo, int offset_us);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_get_data_offset_time(ushort CardNo,ref int offset_us);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_check_done_multicoor_extern(ushort CardNo, ushort Crd,ref ushort crd_state,ref UInt32 crd_stop_reason, ref UInt32 axis_stop_reason);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_error_description(int errcocode, byte[] description);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_write_outport_mask(ushort CardNo, ushort port, UInt32 mask, UInt32 state, UInt32 reverse_time_ms);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_pso_output_delay(ushort CardNo, ushort axis, UInt32 delay_cycle);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pso_output_delay(ushort CardNo, ushort axis,ref UInt32 delay_cycle);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_gap_cmp_space(ushort CardNo, ushort crd, double space);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_gap_cmp_space(ushort CardNo, ushort crd,ref double space);

        [DllImport("LTDMC.dll")]
        public static extern short nmc_ecat_read_slave_register(ushort CardNo, ushort wSlaveAddress, ushort wRegisterOffset, ushort wLen, byte[] pdwData);
        [DllImport("LTDMC.dll")]
        public static extern short nmc_ecat_write_slave_register(ushort CardNo, ushort wSlaveAddress, ushort wRegisterOffset, ushort wLen, byte[] pdwData);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_inp_map_input(ushort CardNo, ushort axis, ushort enable, ushort index, ushort sub_index, ushort bit_index, ushort inp_validvalue, ushort connect2checkdone);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_inp_map_input(ushort CardNo, ushort axis,ref ushort enable, ref ushort index, ref ushort sub_index, ref ushort bit_index, ref ushort inp_validvalue, ref ushort connect2checkdone);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_pwm_state(ushort CardNo, ushort channel,ref ushort state);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_rotation_axis_transform_param(ushort CardNo, ushort axis, double rod_len);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_rotation_axis_transform_param(ushort CardNo, ushort axis,ref double rod_len);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_rotation_axis_transform_enable(ushort CardNo, ushort axis, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_rtcp_get_rotation_axis_transform_enable(ushort CardNo, ushort axis,ref ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_circle_move_3point_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Mid_Pos, long Circle, ushort posi_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_circle_move_3point_unit(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Mid_Pos, long Circle, ushort posi_mode, long mark);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_cam_in(ushort CardNo, ushort slave_axis,ref ushort master_axis,ref ushort execute,ref ushort conti_update, ref ushort cam_table,ref ushort periodic, ref ushort master_abs,ref ushort
 slave_abs,ref double master_offset, ref double slave_offset, ref double master_scaling, ref double slave_scaling, ref double master_start_dist, ref double master_sync_pos, ref double active_pos,ref ushort 
acvive_mode,ref ushort start_mode,ref double velocity,ref double acc,ref double dec,ref double jerk,ref ushort master_source,ref ushort buffer_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_mc_phasing(ushort CardNo, ushort slave_axis, ushort master_axis, ushort execute, double phase_shift, double velocity, double acc, double dec, double jerk);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_mc_phasing_status(ushort CardNo, ushort slave_axis,ref ushort done, ref ushort busy, ref ushort cmd_aborted, ref ushort error, ref UInt32  error_id);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_mc_phasing(ushort CardNo, ushort slave_axis,ref ushort master_axis, ref ushort execute,ref double phase_shift,ref double velocity,ref double acc,ref double dec,ref double jerk);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_pwm_follow_speed(ushort CardNo, ushort pwm_no, ushort axis, ushort mode, double max_vel, double min_vel, double max_value, double out_value, double min_value, ushort 
min_ctl_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_pwm_follow_speed(ushort CardNo, ushort pwm_no, ref ushort axis, ref ushort mode, ref double max_vel, ref double min_vel, ref double max_value, ref double out_value, ref double 
min_value, ref ushort min_ctl_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_pwm_follow_speed_enable(ushort CardNo, ushort pwm_no, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_pwm_follow_speed_enable(ushort CardNo, ushort pwm_no,ref ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_umove_unit(ushort CardNo, ushort group, ushort mode, ushort sources, ushort io_index, ushort io_value, ushort up_axis, double up_pos, double up_safe_distance, ushort move_num, ushort[] 
move_axis_list, double[] move_pos, double[] move_safe_distance, ushort down_axis, double down_pos, ushort posi_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_umove_runsts(ushort CardNo, ushort group,ref ushort runsts);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_umove_stop(ushort CardNo, ushort group);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_knife_positioned(ushort CardNo, ushort Crd, double SecondVel, double SecondPos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_knife_positioned(ushort CardNo, ushort Crd,ref double SecondVel,ref double SecondPos);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_knife_positioned_enable(ushort CardNo, ushort Crd, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_knife_positioned_enable(ushort CardNo, ushort Crd,ref ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_return_to_zero(ushort CardNo, ushort axis);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_lookahead_path_error(ushort CardNo, ushort Crd, double patherr);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_get_lookahead_path_error(ushort CardNo, ushort Crd,ref double patherr);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_safe_pause_list(ushort CardNo, ushort Crd, ushort safe_axis_num, ushort[] safe_axis_list, double[] distance, double[] vstart, double[] vsteady, double[] vend, double[] acc_time, 
double[] dec_time, ushort posi_mode);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_da_follow_speed(ushort CardNo, ushort da_channel, ushort axis, ushort mode, double max_vel, double min_vel, double max_value, double min_value, double offset);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_da_follow_speed(ushort CardNo, ushort da_channel, ref ushort axis,ref ushort mode,ref double max_vel,ref double min_vel,ref double max_value,ref double min_value,ref double 
offset);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_da_follow_speed_enable(ushort CardNo, ushort da_channel, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_da_follow_speed_enable(ushort CardNo, ushort da_channel,ref ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_force_set_position(ushort CardNo, ushort Crd, ushort axis_num, ushort[] axis_list, double[] position);



        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_axis_da_follow_speed_extern(ushort CardNo, ushort da_channel, ushort axis, ushort mode, ushort segment, double[] vel, double[] value);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_axis_da_follow_speed_extern(ushort CardNo, ushort da_channel,ref ushort axis,ref ushort mode,ref ushort segment, double[] vel, double[] value);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_node_da_enable(ushort CardNo, ushort Crd, ushort node_id, ushort channel, ushort enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_node_da_output(ushort CardNo, ushort Crd, ushort node_id, ushort channel, double Vout);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_gantry_move(ushort CardNo, ushort Crd, ushort master_axis, ushort slave_num, ushort[] slave_axis_list, ushort on_off);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_gantry_error_protect_unit(ushort CardNo, ushort Crd, ushort master_axis, double dstp_err, double emg_err, ushort on_off);


        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_sigaxis_moveseg_data_ex(ushort CardNo, ushort group, ushort Axis, double Target_pos, UInt32 mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_wait_event_data(ushort CardNo, ushort group, ushort event1, ushort num,ushort CompareOperator, double target_value, ushort mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_trigger_data(ushort CardNo, ushort group, ushort mode, ushort num, double Target_Value, UInt32 mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_m_add_time_delay(ushort CardNo, ushort group, double Time_delay, UInt32 mark);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_feedforward_profile(ushort CardNo, ushort Axis, double vel_offset_coef, double tor_offset_coef);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_feedforward_profile(ushort CardNo, ushort Axis,ref double vel_offset_coef,ref double tor_offset_coef);
       
		
		
		//¸üĞÂÍ·ÎÄ¼ş20240625
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_modulo_profile(ushort CardNo, ushort Axis, ushort enable,  double Modulo_Vel);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_modulo_profile(ushort CardNo, ushort Axis,ref ushort enable,ref double Modulo_Vel);
		
        [DllImport("LTDMC.dll")]
		public static extern short dmc_line_mutli_line(ushort CardNo, ushort Crd, ushort AxisNum,  ushort[] AxisList, ushort PointNum, double[,] pos, ushort wait_mark, ushort wait_enable, ushort posi_mode);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_line_unit_G0 (ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] pPosList, ushort posi_mode, ushort mark);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_arc_move_center_unit_G0 (ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Cen_Pos,ushort Arc_Dir, ushort Circle, ushort posi_mode, ushort mark);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_arc_move_radius_unit_G0 (ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, ushort Arc_Radius, ushort Arc_Dir, ushort Circle, ushort posi_mode, ushort mark);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_arc_move_3points_unit_G0 (ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Mid_Pos, ushort Circle, ushort posi_mode, ushort mark);

        //×ÜÏß¿¨ĞÂÔö¶ÁÈ¡ËùÓĞIO×´Ì¬¹¦ÄÜ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_outport_array(ushort CardNo, ushort portNum,  uint[] status);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_read_inport_array(ushort CardNo, ushort portNum,  uint[] status);

        //ÉèÖÃ/¶ÁÈ¡Ö¸Áî»º´æÔË¶¯±êÖ¾Î»×´Ì¬
        [DllImport("LTDMC.dll")]
		 public static extern short dmc_m_set_wait_flag(ushort CardNo, ushort Group, ushort FlagNo, ushort Wait_Flag);
		[DllImport("LTDMC.dll")]
		 public static extern short dmc_m_get_wait_flag(ushort CardNo, ushort Group, ushort FlagNo, ref ushort Wait_Flag);

        //·´Ïò¼äÏ¶¹¦ÄÜÀ©Õ¹£¬Ö§³Ö¶àÖÖ²¹³¥Ä£Ê½
		[DllImport("LTDMC.dll")]
		 public static extern short dmc_set_backlash_unit_extern(ushort CardNo, ushort axis,double backlash, ushort mode, short dir,double vel,double acc,ushort time_ms);
		[DllImport("LTDMC.dll")]
		 public static extern short dmc_get_backlash_unit_extern(ushort CardNo, ushort axis,ref double backlash, ref ushort mode, ref short dir,ref double vel, ref double acc,ref ushort time_ms);

        //ÎŞĞèÓ³ÉäÖ±½ÓÊ¹ÓÃPDOµÄ·½Ê½²Ù×÷¶ÔÏó×Öµä¹¦ÄÜ
		[DllImport("LTDMC.dll")]
		 public static extern short nmc_write_rxpdo(ushort CardNo, ushort portnum, ushort slave_station_addr, ushort index, ushort subindex, ushort bitlength, byte[] data);
		[DllImport("LTDMC.dll")]		 
		 public static extern short nmc_read_rxpdo(ushort CardNo, ushort portnum, ushort slave_station_addr, ushort index, ushort subindex, ushort bitlength, byte[] data);
		[DllImport("LTDMC.dll")]
		 public static extern short nmc_read_txpdo(ushort CardNo, ushort portnum, ushort slave_station_addr, ushort index, ushort subindex, ushort bitlength, byte[] data);
		 
		[DllImport("LTDMC.dll")]
		public static extern short dmc_syncmotion_set_enable(ushort CardNo, ushort slaveAxisNo, ushort enable);
		[DllImport("LTDMC.dll")]	
		public static extern short dmc_syncmotion_get_enable(ushort CardNo, ushort slaveAxisNo, ref ushort enable);
		[DllImport("LTDMC.dll")]	
		public static extern short dmc_set_syncmotion_configparams (ushort CardNo, ushort masterAxisNo, ushort slaveAxisNo, ushort follow_src_sel, ushort master_type, double scale_coe, ushort dir_rev);
		[DllImport("LTDMC.dll")]	
		public static extern short dmc_get_syncmotion_configparams (ushort CardNo, ushort slaveAxisNo, ref ushort masterAxisNo, ref ushort follow_src_sel, ref ushort master_type, ref double scale_coe, ref ushort dir_rev);
		[DllImport("LTDMC.dll")]			
		public static extern short dmc_syncmotion_cancle(ushort CardNo, ushort slaveAxisNo, double dec, double jerk);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_syncmotion_updatescale(ushort CardNo, ushort slaveAxisNo, double scale_coe);

        //ÉèÖÃ alm ĞÅºÅÌØÊâ¿ØÖÆ¹¦ÄÜ£º¿ÉÊµÏÖ±¨¾¯ºó¶Ï¿ªÊ¹ÄÜ
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_alm_control_function(ushort CardNo,ushort axis,ushort control_function);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_alm_control_function(ushort CardNo,ushort axis,ref ushort control_function);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_calculate_axis_plan_time(ushort CardNo, double Dis,double Start_Vel, double Max_Vel,double End_Vel,double Tacc, double Tdec,double sTime, ref double Tsum);

        // Âö³å¿¨2D²¹³¥¹¦ÄÜ
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_leadscrew_comp_2D_config_unit(UInt16 CardNo, UInt16 comp_axis, UInt16[] ref_axis, double[] ref_axis_start_pos, double[] ref_axis_length, UInt16[] ref_axis_segment, double[,] value);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_leadscrew_comp_2D_config_unit(UInt16 CardNo, ref UInt16 comp_axis, ref UInt16[] ref_axis, ref double[] ref_axis_start_pos, ref double[] ref_axis_length, ref UInt16[] ref_axis_segment, ref double[,] value);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_leadscrew_comp_2D_angle_unit(UInt16 CardNo, UInt16 comp_axis, UInt16[] ref_axis, double[] ref_axis_start_pos, double[] ref_axis_length, double angle);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_leadscrew_comp_2D_angle_unit(UInt16 CardNo, ref UInt16 comp_axis, ref UInt16[] ref_axis, ref double[] ref_axis_start_pos, ref double[] ref_axis_length, ref double angle);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_leadscrew_comp_2D_enable(UInt16 CardNo, UInt16 comp_axis, UInt16 mode, UInt16 enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_leadscrew_comp_2D_enable(UInt16 CardNo, UInt16 comp_axis, ref UInt16 mode, ref UInt16 enable);

        //¹ì¼£Ëã·¨Éı¼¶Á¬Ğø²å²¹ĞÂÔö¹¦ÄÜ£º°üÀ¨¼õËÙ½ÇÍ£Ö¹½Ç¡¢¹ÕÍäÊ±¼ä¡¢Ğ¡Ô²ÏŞËÙ¡¢ÏÎ½ÓµãËÙ¶È¹æ»®Ä£Ê½ÉèÖÃµÈ¹¦ÄÜ
		[DllImport("LTDMC.dll")]
		 public static extern short dmc_conti_set_coordinate_params(ushort CardNo, ushort Crd, double T, double radius, double limit_vel);
		[DllImport("LTDMC.dll")]
		 public static extern short dmc_conti_get_coordinate_params(ushort CardNo, ushort Crd, ref double T, ref double radius,ref double limit_vel);
		[DllImport("LTDMC.dll")]
		 public static extern short dmc_conti_set_corner_angle_param(ushort CardNo, ushort Crd, double dec_angle, double stop_angle, ushort enable);
		[DllImport("LTDMC.dll")]
		 public static extern short dmc_conti_get_corner_angle_param(ushort CardNo, ushort Crd, ref double dec_angle, ref double stop_angle, ref ushort enable); 
		[DllImport("LTDMC.dll")]
		 public static extern short dmc_conti_set_transvelocity_mode(ushort CardNo, ushort Crd, ushort transvel_mode); 
		[DllImport("LTDMC.dll")]
		 public static extern short dmc_conti_get_transvelocity_mode(ushort CardNo, ushort Crd, ref ushort transvel_mode); 

		[DllImport("LTDMC.dll")]
		public static extern short dmc_send_pack(ushort CardNo, ushort mode, ushort length,byte[] pBuf);
		
		[DllImport("LTDMC.dll")]		
		public static extern short dmc_conti_delay_set_mode(ushort CardNo, ushort Crd, ushort mode);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_delay_get_mode(ushort CardNo, ushort Crd, ref ushort  mode);

        //ÎïÀí½ÚµãÊ¹ÄÜ¹¦ÄÜ
		[DllImport("LTDMC.dll")]
		public static extern short nmc_set_alias_address_enable(ushort CardNo, ushort enable);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_get_alias_address_enable(ushort CardNo, ref ushort  enable, ref ushort  states);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_system_version(ushort CardNo, byte[] SystemVersion);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_track_config_unit(ushort CardNo, ushort m_slave_axis_num, ushort[] m_master_axis, ushort[] m_slave_axis,ushort[] m_start_distance, ushort[] m_coordinate_axis, double[] m_angle, double[] m_master_vel, double[] m_start_time, double[] m_finish_time, double[] m_sync_start_pos, double[] m_sync_end_pos,double[] m_finish_pos);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_track_config_unit(ushort CardNo, ushort slave_axis,ref ushort m_track_state);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_add_move_config_unit(ushort CardNo, ushort add_axis, ushort added_axis, ushort enable);

		[DllImport("LTDMC.dll")]
		public static extern short nmc_get_same_alias_address_slaves(ushort CardNo, ref ushort SlaveNum, ushort[] SlaveList);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_input_shaper_on(ushort CardNo,ushort axis, double[] cnvA, UInt32[] cnvT,ushort num);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_input_shaper_off (ushort CardNo,ushort axis);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_input_shaper_status(ushort CardNo, ushort axis,  ref ushort status);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_input_shaper_compesation_enable(ushort CardNo,ushort axis, ushort enable);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_input_shaper_compesation_enable(ushort CardNo,ushort axis, ref ushort enable);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_input_shaper_compesation(ushort CardNo,ushort axis, double T,double freq,double zeta,double alpha);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_input_shaper_compesation(ushort CardNo,ushort axis, ref double T, ref double freq, ref double zeta,ref double alpha);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_input_shaper_compesation_error(ushort CardNo, ushort axis, double delay_T, double pos_error);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_input_shaper_compesation_error(ushort CardNo, ushort axis, ref double delay_T, ref double pos_error);
		
		[DllImport("LTDMC.dll")]
		public static extern short dmc_m_add_sigaxis_moveseg_data_multi(ushort CardNo, ushort group, ushort AxisNum, ushort[] AxisList, double[] Target_pos, uint[] mark);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_func_enable(ushort CardNo, ushort mode, ushort enable);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_func_enable(ushort CardNo, ushort mode, ref ushort enable);

        //×ÜÏß»ØÁã²ÎÊıÉèÖÃÖ§³Ö¼ÓËÙ¶ÈÄ£Ê½
		[DllImport("LTDMC.dll")]
		public static extern short nmc_set_home_profile_acc(ushort CardNo, ushort axis, ushort home_mode,double Low_Vel, double High_Vel,double Tacc,double Tdec,double offsetpos );
		[DllImport("LTDMC.dll")]
		public static extern short nmc_get_home_profile_acc(ushort CardNo, ushort axis, ref ushort home_mode, ref double Low_Vel, ref double High_Vel, ref double Tacc, ref double Tdec,ref double Offsetpos);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_ad_input_append(ushort CardNo,ushort Channel, ref double Vout);

        //PWM¿ªÊ¼ÓÅÏÈÊä³öµçÆ½²¿·ÖÉèÖÃ¹¦ÄÜ
        [DllImport("LTDMC.dll")]
		public static extern short dmc_conti_set_pwm_output_extern(ushort CardNo, ushort Crd, ushort pwm_no, ushort enable, double width_us, double frequency,uint number);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_pwm_mode(ushort CardNo, ushort pwm_no, ushort startmode, ushort stopmode);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_pwm_mode (ushort CardNo, ushort pwm_no, ref ushort startmode, ref ushort stopmode);

        //Á¬Ğø²å²¹µ¥ÖáÔË¶¯Ö§³ÖËÙ¶È»º´æÉèÖÃ¹¦ÄÜ
		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_set_profile_unit(ushort CardNo,ushort Crd,double Min_Vel,double Max_vel,double Tacc,double Tdec,double Stop_Vel);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_pmove_extern_unit(ushort CardNo, ushort Crd, ushort axis, double dist, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel, ushort posi_mode, ushort mode, int mark);

        //Á¬Ğø²å²¹ÅúÁ¿Ìí¼Ó¹¦ÄÜ
		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_pack_on(ushort CardNo);
		[DllImport("LTDMC.dll")]		
		public static extern short dmc_conti_pack_off(ushort CardNo);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_pack_flush(ushort CardNo);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_sine_oscillate_extern(ushort CardNo,ushort Axis,double Amplitude,double Frequency,double cycle,ushort param);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_sine_oscillate_extern_unit(ushort CardNo,ushort Axis,double Amplitude,double Frequency,double cycle,ushort param);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_write_outport_array(ushort CardNo, ushort portNum,  uint[] status);

        //Á¬Ğø²å²¹ÖĞ»º´æ²Ù×÷SDO/PDO¹¦ÄÜ
        [DllImport("LTDMC.dll")]
		public static extern short dmc_conti_set_node_od(ushort CardNo,ushort Crd,ushort NodeNum, ushort Index,ushort SubIndex,ushort ValLength, uint Value);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_set_rxpdo_extra(ushort CardNo, ushort Crd, ushort Address, ushort DataLen, ushort Mode, ushort ModeVal, uint Value);

        //Ö¸Áî»º´æÖĞĞÂÔö²Ù×÷SDO/PDO¹¦ÄÜ
        [DllImport("LTDMC.dll")]
		public static extern short dmc_m_add_trigger_set_od(ushort CardNo,ushort Group,ushort NodeNum, ushort Index,ushort SubIndex,ushort ValLength,uint Value, uint Mark);
        [DllImport("LTDMC.dll")]
		public static extern short dmc_m_add_trigger_set_rxpdo_extra(ushort CardNo,ushort Group,ushort Address, ushort DataLen,ushort Mode,ushort ModeVal,uint Value,uint Mark);

        //²å²¹µ¥ÖáÏŞËÙÊ¹ÄÜ
	   [DllImport("LTDMC.dll")]
		public static extern short dmc_set_profile_limit_by_axis(ushort CardNo, ushort axis, ushort enable);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_profile_limit_by_axis(ushort CardNo, ushort axis, ref ushort enable);

        [DllImport("LTDMC.dll")]//×ÜÏß2D²¹³¥¹¦ÄÜ
        public static extern short dmc_set_leadscrew_comp_2D_config_unit_ex(UInt16 CardNo, UInt16 table_index, UInt16 comp_axis, UInt16[] ref_axis, double[] ref_axis_start_pos, double[] ref_axis_length, UInt16[] ref_axis_segment, double[] value);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_leadscrew_comp_2D_config_unit_ex(UInt16 CardNo, UInt16 table_index, ref UInt16 comp_axis, ref UInt16[] ref_axis, ref double[] ref_axis_start_pos, ref double[] ref_axis_length, ref UInt16[] ref_axis_segment, ref double[] value);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_leadscrew_comp_2D_angle_unit_ex(UInt16 CardNo, UInt16 table_index, UInt16 comp_axis, UInt16[] ref_axis, double[] ref_axis_start_pos, double[] ref_axis_length, double angle);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_leadscrew_comp_2D_angle_unit_ex(UInt16 CardNo, UInt16 table_index, ref UInt16 comp_axis, ref UInt16[] ref_axis, ref double[] ref_axis_start_pos, ref double[] ref_axis_length, ref double angle);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_set_leadscrew_comp_2D_enable_ex(UInt16 CardNo, UInt16 table_index, UInt16 mode, UInt16 enable);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_leadscrew_comp_2D_enable_ex(UInt16 CardNo, UInt16 table_index, ref UInt16 mode, ref UInt16 enable);

        //EMCÏµÁĞ¿ØÖÆÆ÷ipµØÖ·ÉèÖÃºÍ»ñÈ¡¹¦ÄÜ
        [DllImport("LTDMC.dll")]		
		 public static extern short dmc_set_ipaddr( ushort CardNo, byte[]  IpAddr);
        [DllImport("LTDMC.dll")]
		public static extern short dmc_get_ipaddr( ushort CardNo, byte[] IpAddr);
		
        //IO·­×ª¶¯×÷Çå³ı¹¦ÄÜ
        [DllImport("LTDMC.dll")]
		 public static extern short dmc_clear_io_action(ushort CardNo,ushort Crd,ushort IoMask, ushort Mode);

        [DllImport("LTDMC.dll")]
		public static extern short dmc_m_remain_space(ushort CardNo, ushort Crd, uint[] data);
		
        [DllImport("LTDMC.dll")]		
		public static extern short dmc_conti_set_trans_arc_speed_mode(ushort CardNo, ushort Crd, ushort mode);
        [DllImport("LTDMC.dll")]
		public static extern short dmc_conti_get_trans_arc_speed_mode(ushort CardNo, ushort Crd, ref ushort mode);

        //5X10Á¬Ğø²å²¹ÖĞ CAN Ä£¿é DA ËÙ¶È¸úËæ¹¦ÄÜ
        [DllImport("LTDMC.dll")]
		public static extern short dmc_conti_set_node_da_follow_speed(ushort CardNo, ushort Crd, ushort node_id, ushort da_no, double MaxVel, double MaxValue, double acc_offset, double dec_offset, double acc_dist, double dec_dist);
        [DllImport("LTDMC.dll")]
		public static extern short dmc_conti_get_node_da_follow_speed(ushort CardNo, ushort Crd, ushort node_id, ushort da_no, ref double MaxVel, ref double MaxValue, ref double acc_offset, ref double dec_offset, ref double acc_dist, ref double dec_dist);
        
		[DllImport("LTDMC.dll")]
		public static extern short dmc_m_set_factor_error(ushort CardNo, ushort axis, ushort enable, double Positive_error, double Negative_error, ushort retain);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_rAxis_comp_config(ushort CardNo,short axis, short enable, int cnt, double[] angle, double[] xPos, double[] yPos);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_rAxis_comp_loop(ushort CardNo,short axis, short mode, double angle, double xPos, double yPos, double[] new_xPos, double[] new_yPos);

        //µãÎ»ÔË¶¯Pro¹æ»®Ä£Ê½¹¦ÄÜ
        [DllImport("LTDMC.dll")]
		public static extern short dmc_pmove_pro_unit(ushort CardNo, ushort axis_no, double pos, ushort pos_mode, ushort plan_mode, byte[] vel_param, byte[] plan_result);

        //5X10ÏµÁĞÁ¬Ğø²å²¹ÖĞÏà¶ÔÓÚ¹ì¼£¶ÎÖÕµã DA ÌáÇ°/ÖÍºó¸úËæÊä³ö
		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_delay_node_da_follow_to_start(ushort CardNo, ushort Crd, ushort node_id, ushort da_no, ushort delay_mode, double delay_value, double min_value, double max_value, double period);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_ahead_node_da_follow_to_stop(ushort CardNo, ushort Crd, ushort node_id, ushort da_no, ushort ahead_mode, double ahead_value, double min_value, double max_value, double period);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_printall_time();
		[DllImport("LTDMC.dll")]
		public static extern short dmc_clear_time();

		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_pmove_consumed_time (ushort CardNo,ref ushort time_receive, ref ushort time_process, ref ushort time_send);

        //¿ØÖÆ¿¨ÔË¶¯²ÎÊıÉèÖÃ¼ÓËÙ¶ÈÄ£Ê½½Ó¿Ú
		[DllImport("LTDMC.dll")]
		public static extern short dmc_t_pmove_extern_unit_acc(ushort CardNo, ushort axis, double MidPos,double TargetPos, double Min_Vel,double Max_Vel, double stop_Vel, double acc,double dec,ushort posi_mode);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_t_pmove_extern_acc(ushort CardNo, ushort axis, double MidPos,double TargetPos, double Min_Vel,double Max_Vel, double stop_Vel, double acc,double dec,ushort posi_mode);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_t_pmove_extern_softstart_unit_acc(ushort CardNo, ushort axis, double MidPos, double TargetPos, double start_Vel, double Max_Vel, double stop_Vel, uint delay_ms, double Max_Vel2,double stop_vel2, double acc_time, double dec_time, ushort posi_mode);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_t_pmove_extern_softstart_acc(ushort CardNo, ushort axis, double MidPos, double TargetPos, double start_Vel, double Max_Vel, double stop_Vel, uint delay_ms, double Max_Vel2,double stop_vel2, double acc_time, double dec_time, ushort posi_mode);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_change_speed_unit_acc(ushort CardNo,ushort axis, double New_Vel,double Taccdec);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_change_speed_acc(ushort CardNo,ushort axis,double Curr_Vel,double Taccdec);

        //5X10ÏµÁĞÁ¬Ğø²å²¹ CANÄ£¿éDA ¸úËæ¹¦ÄÜ
		[DllImport("LTDMC.dll")]	
		public static extern short dmc_set_node_da_follow_speed_extern(ushort CardNo, ushort node_id, ushort da_channel, ushort axis, ushort mode, ushort segment, double[] vel,double[] value); 
		[DllImport("LTDMC.dll")]	
		public static extern short dmc_get_node_da_follow_speed_extern(ushort CardNo, ushort node_id, ushort da_channel, ref ushort axis, ref ushort mode, ref ushort segment, double[] vel,double[] value);
		[DllImport("LTDMC.dll")]	
		public static extern short dmc_set_node_da_follow_speed_enable(ushort CardNo, ushort node_id, ushort da_channel, ushort enable); 
		[DllImport("LTDMC.dll")]	
		public static extern short dmc_get_node_da_follow_speed_enable(ushort CardNo, ushort node_id, ushort da_channel, ref ushort enable); 

		[DllImport("LTDMC.dll")]	
		public static extern short  nmc_set_clear_fieldbus_state_on_soft_reset(ushort CardNo, ushort enable);
		[DllImport("LTDMC.dll")]	
		public static extern short  nmc_get_clear_fieldbus_state_on_soft_reset(ushort CardNo, ref ushort enable);

		[DllImport("LTDMC.dll")]	
		public static extern short dmc_m_set_profile_unit_acc(ushort CardNo, ushort group, ushort axis, double start_vel, double max_vel, double tacc, double tdec, double stop_vel);
		[DllImport("LTDMC.dll")]	
		public static extern short dmc_m_get_profile_unit_acc(ushort CardNo, ushort group, ushort axis, ref double start_vel, ref double max_vel, ref double tacc, ref double tdec, ref double stop_vel);

		[DllImport("LTDMC.dll")]
		public static extern short nmc_get_master_state(ushort CardNo, ref uint States);



		//»·ÍøÈßÓàÏà¹Ø×´Ì¬
		[DllImport("LTDMC.dll")]
        public static extern short nmc_get_cable_redundancy_enable(ushort CardNo, ref ushort Enable, ref ushort ErrStop);
		[DllImport("LTDMC.dll")]
        public static extern short nmc_get_num_connected_slaves_red(ushort CardNo, ref uint BrkMainSlaves, ref uint BrkRedSlaves, ref uint CurMainSlaves, ref uint CurRedSlaves);
		
		[DllImport("LTDMC.dll")]
		public static extern short get_bar2_value(ushort CardNo, ushort offset , ushort length, uint[] value);

        //»ñÈ¡±àÂëÆ÷ËÙ¶È¹¦ÄÜ
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_encoder_speed(ushort CardNo, ushort axis, ref double vel);

        public struct PwmCurve_CtrlPoint
        {
            public float fl_val;//pwmËæ¶¯Öµ£¨Òò±äÁ¿£©
            public float ctrl_val;//¿ØÖÆÖµ£¨×Ô±äÁ¿£©
        }
        public struct DaCurve_CtrlPoint
        {
            public float vol_val;//daÖµ£¨Òò±äÁ¿£©
            public float ctrl_val;//¿ØÖÆÖµ£¨×Ô±äÁ¿£©
        }
        //5X10ÏµÁĞÁ¬Ğø²å²¹DA--T/P¸úËæÊä³ö¹¦ÄÜ
        [DllImport("LTDMC.dll")]
		public static extern short dmc_set_da_curve(ushort CardNo, ushort Crd, ushort curve_no, ushort curve_type,ushort ctrl_point_num, DaCurve_CtrlPoint[] ctrl_point);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_get_da_curve(ushort CardNo, ushort Crd, ushort curve_no, ref ushort curve_type, ref ushort ctrl_point_num, DaCurve_CtrlPoint[] ctrl_point);

        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_da_curve_control_to_start(ushort CardNo, ushort Crd, ushort curve_no, ushort da_channel, ushort delay_mode, float delay_value, uint mark);
        [DllImport("LTDMC.dll")]
        public static extern short dmc_conti_da_curve_control_to_stop(ushort CardNo, ushort Crd, ushort curve_no, ushort da_channel, ushort ahead_mode, float ahead_value, uint mark);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_da_curve_control(ushort CardNo, ushort Crd, ushort curve_no, ushort on_off,ushort da_channel,uint us_delay_time,uint mark);

		[DllImport("LTDMC.dll")]
        public static extern short dmc_cmd_buf_set_axis_profile_ext(ushort CardNo, ushort group, ushort axis_num, ushort[] axis_list, ushort[] plan_mode, double[] start_vel, double[] max_vel, double[] stop_vel, double[] tacc, double[] tdec, double[] ja, double[] jd);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_node_da_output (ushort CardNo,ushort PortNum,ushort NodeNum, ushort Group,ushort[] Target_Time,ushort[] Target_Voltage);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_node_DA_configure(ushort CardNo,ushort PortNum,ushort NodeNum, ushort Channel,ushort Group);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_node_DA_enable(ushort CardNo,ushort PortNum,ushort NodeNum, ushort Channel,ushort DelayPeriod);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_node_DA_stop(ushort CardNo,ushort PortNum,ushort NodeNum, ushort Channel);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_node_DA_mode(ushort CardNo,ushort Mode, ushort Address,ushort DataLen,ushort Value, ushort DelayPeriod);

		[DllImport("LTDMC.dll")]		
		public static extern short dmc_set_acc_jerkratio_profile_unit (ushort CardNo, ushort axis, double Min_Vel, double Max_Vel, double averageAcc, double averageDec, double jerkAccRatio, double jerkDecRatio ,double stop_vel);
		[DllImport("LTDMC.dll")]
        public static extern short dmc_get_acc_jerkratio_profile_unit(ushort CardNo, ushort axis, ref double Min_Vel, ref double Max_Vel, ref double averageAcc, ref double averageDec, ref double jerkAccRatio, ref double jerkDecRatio, ref double stop_vel);
		[DllImport("LTDMC.dll")]		
		public static extern short dmc_set_acctime_jerkratio_profile_unit (ushort CardNo, ushort axis, double Min_Vel, double Max_Vel, double accTime, double decTime, double jerkAccRatio, double jerkDecRatio ,double stop_vel);
		[DllImport("LTDMC.dll")]
        public static extern short dmc_get_acctime_jerkratio_profile_unit(ushort CardNo, ushort axis, ref double Min_Vel, ref double Max_Vel, ref double accTime, ref double dectime, ref double jerkAccRatio, ref double jerkDecRatio, ref double stop_vel);
		[DllImport("LTDMC.dll")]		
		public static extern short dmc_set_vector_acc_jerkratio_profile_unit (ushort CardNo, ushort CrdID, double Min_Vel, double Max_Vel, double averageAcc, double averageDec, double jerkAccRatio, double jerkDecRatio ,double stop_vel);
		[DllImport("LTDMC.dll")]
        public static extern short dmc_get_vector_acc_jerkratio_profile_unit(ushort CardNo, ushort CrdID, ref double Min_Vel, ref double Max_Vel, ref double averageAcc, ref double averageDec, ref double jerkAccRatio, ref double jerkDecRatio, ref double stop_vel);
		[DllImport("LTDMC.dll")]		
		public static extern short dmc_set_vector_acctime_jerkratio_profile_unit (ushort CardNo, ushort axis, double Min_Vel, double Max_Vel, double accTime, double decTime, double jerkAccRatio, double jerkDecRatio ,double stop_vel);
		[DllImport("LTDMC.dll")]
        public static extern short dmc_get_vector_acctime_jerkratio_profile_unit(ushort CardNo, ushort CrdID, ref double Min_Vel, ref double Max_Vel, ref double accTime, ref double decTime, ref double jerkAccRatio, ref double jerkDecRatio, ref double stop_vel);

		[DllImport("LTDMC.dll")]	
		public static extern short dmc_measure_time_enable(ushort CardNo,ushort enable);
		
		[DllImport("LTDMC.dll")]
        public static extern short dmc_m_cmd_buf_set_profile_extern(ushort CardNo, ushort group, ushort axis_num, ushort[] axis_list, ushort[] plan_mode, double[] start_vel, double[] max_vel, double[] stop_vel, double[] tacc, double[] tdec, double[] ja, double[] jd);
        //Âö³å¿¨»ØÁã¹¦ÄÜÀ©Õ¹£º»ØÁãÔ­µãºÍEZĞÅºÅÖ§³ÖÉèÖÃÑ°ÕÒ¾àÀë
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_home_search_limit(ushort CardNo, ushort axis, ushort enable, ushort stop_mode, uint limit_length, uint org_length,uint ez_length);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_home_search_limit_unit(ushort CardNo, ushort axis, ushort enable, ushort stop_mode, double limit_length,double org_length, double ez_length);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_traj_algorithm_version(ushort CardNo,byte[] trajAlgorithmVersion);

        //×ÜÏß¿¨ÊÊÅäISMCÇı¶¯Æ÷Èí×ÅÂ½¶¨ÖÆ¹¦ÄÜ
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_pt_soft_land_profile(ushort CardNo, ushort AxisNo, int PpModePos, int ChangePos, uint PosTorLimit, uint DelayTime, uint KeepTorque, uint Mode);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_pt_soft_land_init_profile(ushort CardNo, ushort AxisNo, uint PpModeVel, uint PpModeAcc, uint PpModeDec, int SuccessMaxVel, int ReturnPos, int PvModeVel, uint PvModeSecondVel);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_pt_soft_land_arrive_flag(ushort CardNo, ushort AxisNo, ref ushort Flag);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_start_pt_soft_land(ushort CardNo, ushort AxisNo, ushort RunMode);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_pt_soft_land_return(ushort CardNo, ushort AxisNo, ushort RunMode);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_stop_pt_soft_land(ushort CardNo, ushort AxisNo, ushort RunMode);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_clear_pt_soft_land_errcode(ushort CardNo, ushort AxisNo, ushort RunMode);
        
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_traj_min_travel_time(ushort CardNo, ushort Crd, double min_travel_time);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_traj_min_travel_time(ushort CardNo, ushort Crd, ref double min_travel_time);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_set_follow_params(ushort CardNo,ushort Crd,ushort Follow_type, ushort Master, ushort Slave);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_conti_get_follow_params(ushort CardNo,ushort Crd,ushort Follow_type, ushort Master, ref ushort Slave);


		[DllImport("LTDMC.dll")]
        public static extern short nmc_get_errcode_ex(ushort CardNo, ushort Channel, ref uint ErrType, ref uint ErrCode);
		[DllImport("LTDMC.dll")]
        public static extern short nmc_fieldbus_state_get_record_info(ushort CardNo, ushort Channel, uint ReadErrNum, uint ReadErrIndex, ref uint TotalErrNum, ref ushort ReturnErrNum, ref long TimeStamp, ref uint ErrIndex, ref ushort SlaveAddr, ref uint ErrType, ref uint ErrCode, ref uint Info_0, ref uint Info_1, ref uint Info_2, ref uint Info_3);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_fieldbus_state_set_record_stop(ushort CardNo, ushort Stop);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_fieldbus_state_get_record_stop(ushort CardNo, ref ushort Stop);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_fieldbus_state_set_record_mode(ushort CardNo, ushort Mode);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_fieldbus_state_get_record_mode(ushort CardNo, ref ushort Mode);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_set_system_time(ushort CardNo, ushort Year, ushort Mon, ushort Day, ushort Hour, ushort Min, ushort Sec);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_get_system_time(ushort CardNo, ref ushort Year, ref ushort Mon, ref ushort Day, ref ushort Hour, ref ushort Min, ref ushort Sec);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_set_master_state(ushort CardNo, uint States);
		[DllImport("LTDMC.dll")]
        public static extern short nmc_get_master_diagnosis_info(ushort CardNo, ref uint NumSlavesFound, ref uint NumDCSlavesFound, ref uint NumCfgSlaves, ref uint NumMbxSlaves, ref uint TXFrames, ref uint RXFrames, ref uint LostFrames, ref uint CyclicFrames, ref uint CyclicLostFrames, ref uint AcyclicFrames, ref uint AcyclicLostFrames);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_clear_master_diagnosis_info(ushort CardNo, ushort Channel);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_set_slave_port_state(ushort CardNo, ushort SlaveId, ushort Port, ushort State);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_get_slave_port_state(ushort CardNo, ushort SlaveId,ref ushort PortState);
		[DllImport("LTDMC.dll")]
        public static extern short nmc_get_exclude_slave_list(ushort CardNo, ref ushort SlaveNum, ushort[] SlaveList);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_hot_connect_accept_topo_change(ushort CardNo, ushort Channel);
		[DllImport("LTDMC.dll")]
        public static extern short nmc_hot_connect_get_mode(ushort CardNo, ref uint Mode, ref ushort BoardClose, ref ushort ErrStop);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_hot_connect_get_group_num(ushort CardNo, ref uint GroupNum);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_hot_connect_get_group_present(ushort CardNo, uint GroupIndex,ref ushort Present);
		[DllImport("LTDMC.dll")]
        public static extern short nmc_hot_connect_get_all_group_present(ushort CardNo, ref uint GroupNum, ref uint PresentList);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_hot_connect_get_slave_group(ushort CardNo, ushort NodeId, ref uint GroupIndex);
		[DllImport("LTDMC.dll")]
        public static extern short nmc_hot_connect_get_group_slaves(ushort CardNo, uint GroupIndex, ref ushort SlaveNum, ref ushort SlaveList);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_foe_download_file(ushort CardNo, uint SlaveId, uint Password, uint Timeout, string pFileName, byte[] pFileNameControl);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_foe_upload_file(ushort CardNo, uint SlaveId, uint Password, uint Timeout,string pFileName, byte[] pFileNameControl);

        //¿´ÃÅ¹·¹¦ÄÜÀ©Õ¹£ºÖ§³Ö¾Ö²¿Êä³ö¿Ú¸´Î»ÉèÖÃ
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_watchdog_write_outport_mask(ushort CardNo, ulong PortNum, ulong PortValue);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_watchdog_write_outport_mask(ushort CardNo, ulong PortNum, ref ulong PortValue);

        [DllImport("LTDMC.dll")]
		public static extern short dmc_set_watchdog_write_outport_mask_ex(ushort CardNo, ulong PortNum, ulong PortValue);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_watchdog_write_outport_mask_ex(ushort CardNo, ulong PortNum, ref ulong PortValue);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_watchdog_outport_mask_enable(ushort CardNo, ulong PortNum, ulong Enable);
		[DllImport("LTDMC.dll")]
		public static extern short dmc_get_watchdog_outport_mask_enable(ushort CardNo, ulong PortNum, ref ulong Enable);

        //IO¼ÆÊı¹¦ÄÜÀ©Õ¹£ºÖ§³Ö°´ÕÕIO½ÚµãºÅ½øĞĞÉèÖÃ¼ÆÊı
		[DllImport("LTDMC.dll")]
		public static extern short nmc_set_io_count_mode(ushort CardNo, ushort PortNum, ushort NodeID, ushort BitNo, ushort Mode , ushort FilterTime);
		[DllImport("LTDMC.dll")]
        public static extern short nmc_get_io_count_mode(ushort CardNo, ushort PortNum, ushort NodeID, ushort BitNo, ref ushort Mode, ref ushort FilterTime);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_set_io_count_value(ushort CardNo, ushort PortNum, ushort NodeID, ushort BitNo, ushort  CountValue);
		[DllImport("LTDMC.dll")]
		public static extern short nmc_get_io_count_value(ushort CardNo, ushort PortNum, ushort NodeID, ushort BitNo, ref ushort CountValue);

		[DllImport("LTDMC.dll")]
		public static extern short dmc_set_pt_soft_land_PosTorLimit(ushort CardNo, ushort AxisNo, uint PosTorLimit);

		[DllImport("LTDMC.dll")]
		public static extern short nmc_get_exclude_slave_enable (ushort CardNo,ref ushort Enable);


		[DllImport("LTDMC.dll")]
		public static extern short  dmc_m_add_line_moveseg_data_multi(ushort CardNo, ushort group, ushort AxisNum, ushort[] AxisList, double[] Target_pos, long mark);
		[DllImport("LTDMC.dll")]
		public static extern short  dmc_cmd_buf_set_line_profile_ext(ushort CardNo, ushort group, ushort plan_mode, double start_vel, double max_vel, double stop_vel, double tacc, double tdec, double ja,double jd);
	
    }
}
