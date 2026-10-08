// Bridge between Unity (C#) and the YouTube Playables SDK (window.ytgame).
// The SDK itself is loaded by the WebGL template: <script src="https://www.youtube.com/game_api/v1">
mergeInto(LibraryManager.library, {

  YT_IsAvailable: function () {
    return (typeof ytgame !== 'undefined' && ytgame.IN_PLAYABLES_ENV) ? 1 : 0;
  },

  YT_RegisterCallbacks: function (goPtr) {
    var go = UTF8ToString(goPtr);
    try {
      ytgame.system.onPause(function () { SendMessage(go, 'OnYTPause', ''); });
      ytgame.system.onResume(function () { SendMessage(go, 'OnYTResume', ''); });
      ytgame.system.onAudioEnabledChange(function (enabled) {
        SendMessage(go, 'OnYTAudioEnabled', enabled ? '1' : '0');
      });
    } catch (e) {
      console.warn('[CatRoom] ytgame callbacks failed', e);
    }
  },

  YT_GameReady: function () {
    try { ytgame.game.gameReady(); } catch (e) { console.warn('[CatRoom] gameReady failed', e); }
  },

  YT_LoadData: function (goPtr) {
    var go = UTF8ToString(goPtr);
    try {
      ytgame.game.loadData().then(function (data) {
        SendMessage(go, 'OnYTLoadData', data || '');
      }, function (err) {
        SendMessage(go, 'OnYTLoadDataError', String(err));
      });
    } catch (e) {
      SendMessage(go, 'OnYTLoadDataError', String(e));
    }
  },

  YT_SaveData: function (dataPtr) {
    var data = UTF8ToString(dataPtr);
    try {
      ytgame.game.saveData(data).then(function () { }, function (err) {
        console.warn('[CatRoom] saveData failed', err);
        try { ytgame.health.logWarning('saveData failed: ' + err); } catch (e2) { }
      });
    } catch (e) {
      console.warn('[CatRoom] saveData threw', e);
    }
  },

  YT_IsAudioEnabled: function () {
    try { return ytgame.system.isAudioEnabled() ? 1 : 0; } catch (e) { return 1; }
  },

  YT_RequestLanguage: function (goPtr) {
    var go = UTF8ToString(goPtr);
    try {
      ytgame.system.getLanguage().then(function (lang) {
        SendMessage(go, 'OnYTLanguage', lang || '');
      }, function () {
        SendMessage(go, 'OnYTLanguage', '');
      });
    } catch (e) {
      SendMessage(go, 'OnYTLanguage', '');
    }
  },

  YT_SendScore: function (value) {
    try { ytgame.engagement.sendScore({ value: Math.floor(value) }); } catch (e) { }
  },

  YT_LogWarning: function (msgPtr) {
    try { ytgame.health.logWarning(UTF8ToString(msgPtr)); } catch (e) { }
  }
});
