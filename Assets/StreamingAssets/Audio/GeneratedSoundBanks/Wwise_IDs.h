/////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Audiokinetic Wwise generated include file. Do not edit.
//
/////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef __WWISE_IDS_H__
#define __WWISE_IDS_H__

#include <AK/SoundEngine/Common/AkTypes.h>

namespace AK
{
    namespace EVENTS
    {
        static const AkUniqueID BUTTON_BACK_PLAY = 1597374082U;
        static const AkUniqueID BUTTON_CLICK_PLAY = 2856915265U;
        static const AkUniqueID MUSIC_PLAY = 202194903U;
        static const AkUniqueID PLAYER_DANCE_PLAY = 3314422259U;
        static const AkUniqueID PLAYER_DEATH_PLAY = 2348472398U;
        static const AkUniqueID PLAYER_FOOTSTEP_PLAY = 2241160260U;
        static const AkUniqueID PLAYER_JUMP_PLAY = 391280646U;
        static const AkUniqueID PLAYER_LAND_PLAY = 581954083U;
        static const AkUniqueID PLAYER_SWING_PLAY = 3260876866U;
        static const AkUniqueID TRAP_ARROW_HIT_PLAY = 2657231973U;
        static const AkUniqueID TRAP_SPIKE_ACTIVATE_PLAY = 499064214U;
    } // namespace EVENTS

    namespace STATES
    {
        namespace FOCUS
        {
            static const AkUniqueID GROUP = 249970651U;

            namespace STATE
            {
                static const AkUniqueID NONE = 748895195U;
                static const AkUniqueID NORMAL = 1160234136U;
                static const AkUniqueID PAUSED = 319258907U;
            } // namespace STATE
        } // namespace FOCUS

        namespace GAMEPHASE
        {
            static const AkUniqueID GROUP = 768929368U;

            namespace STATE
            {
                static const AkUniqueID COMPLETED = 94054856U;
                static const AkUniqueID COUNTDOWN = 1505888634U;
                static const AkUniqueID FAILED = 1655200910U;
                static const AkUniqueID MENU = 2607556080U;
                static const AkUniqueID NONE = 748895195U;
                static const AkUniqueID PLAYING = 1852808225U;
            } // namespace STATE
        } // namespace GAMEPHASE

    } // namespace STATES

    namespace SWITCHES
    {
        namespace MOVEMENTSTATE
        {
            static const AkUniqueID GROUP = 2240958409U;

            namespace SWITCH
            {
                static const AkUniqueID AIRBORNE = 1785231519U;
                static const AkUniqueID JUMP = 3833651337U;
                static const AkUniqueID RUN = 712161704U;
                static const AkUniqueID WALK = 2108779966U;
            } // namespace SWITCH
        } // namespace MOVEMENTSTATE

        namespace SURFACETYPE
        {
            static const AkUniqueID GROUP = 63790334U;

            namespace SWITCH
            {
                static const AkUniqueID CONCRETE = 841620460U;
                static const AkUniqueID DIRT = 2195636714U;
                static const AkUniqueID METAL = 2473969246U;
                static const AkUniqueID WATER = 2654748154U;
                static const AkUniqueID WOOD = 2058049674U;
            } // namespace SWITCH
        } // namespace SURFACETYPE

    } // namespace SWITCHES

    namespace GAME_PARAMETERS
    {
        static const AkUniqueID GP_DANGERPROXIMITY = 447193833U;
        static const AkUniqueID GP_HEIGHT = 3118997414U;
        static const AkUniqueID GP_PLAYERSPEED = 3075218425U;
        static const AkUniqueID MASTER_VOLUME = 4179668880U;
        static const AkUniqueID MUSIC_VOLUME = 1006694123U;
        static const AkUniqueID PLAYER_VOLUME = 1975814047U;
        static const AkUniqueID SFX_VOLUME = 1564184899U;
    } // namespace GAME_PARAMETERS

    namespace BANKS
    {
        static const AkUniqueID INIT = 1355168291U;
        static const AkUniqueID MAIN = 3161908922U;
    } // namespace BANKS

    namespace BUSSES
    {
        static const AkUniqueID AMB_INDOOR = 3997975935U;
        static const AkUniqueID AMB_OUTDOOR = 1734347146U;
        static const AkUniqueID AMBIENCE = 85412153U;
        static const AkUniqueID AUX_BUSSES = 136159466U;
        static const AkUniqueID MASTER_AUDIO_BUS = 3803692087U;
        static const AkUniqueID MUSIC = 3991942870U;
        static const AkUniqueID SFX = 393239870U;
        static const AkUniqueID SFX_ENVIRONMENT = 573128568U;
        static const AkUniqueID SFX_PLAYER = 217780010U;
        static const AkUniqueID SFX_TAUNTS = 4015076934U;
        static const AkUniqueID SFX_TRAPS = 3795829403U;
        static const AkUniqueID UI = 1551306167U;
    } // namespace BUSSES

    namespace AUX_BUSSES
    {
        static const AkUniqueID REVERB_OUTDOOR = 1578973140U;
    } // namespace AUX_BUSSES

    namespace AUDIO_DEVICES
    {
        static const AkUniqueID NO_OUTPUT = 2317455096U;
        static const AkUniqueID SYSTEM = 3859886410U;
    } // namespace AUDIO_DEVICES

}// namespace AK

#endif // __WWISE_IDS_H__
