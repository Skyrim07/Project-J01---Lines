using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace SKCell
{
    [AddComponentMenu("SKCell/SKAudioManager")]
    public class SKAudioManager : MonoSingleton<SKAudioManager>
    {
        private Dictionary<string, string> audioPathDict;    

        public AudioSource musicAudioSource;

        private List<AudioSource> unusedSoundAudioSourceList;   
        private List<AudioSource> usedSoundAudioSourceList;     
        private Dictionary<string, AudioClip> audioClipDict = new Dictionary<string, AudioClip>();      
        private Dictionary<string, AudioSource> audioSourceDict = new Dictionary<string, AudioSource>();      

        private float musicVolume = 1;
        private float soundVolume = 1;

        private string musicVolumePrefs = "MusicVolume";
        private string soundVolumePrefs = "SoundVolume";

        private int poolCount = 3;       

        private string MUSIC_PATH = "AudioClip/Music/";
        private string SOUND_PATH = "AudioClip/Sound/";
        private string BGM_PATH = "AudioClip/BGM/";

        public AudioSource audioSource = new AudioSource();
        AudioSource MBsource = new AudioSource();

        public List<string> music_ids = new List<string>();
        public List<string> BGMs = new List<string>();

        int BGMhasPlayed = 0;

        private Dictionary<string, int> blackSoundDic;

        private int blackCount = 2;

        protected override void Awake()
        {
            base.Awake();
            musicAudioSource = gameObject.AddComponent<AudioSource>();
            unusedSoundAudioSourceList = new List<AudioSource>();
            usedSoundAudioSourceList = new List<AudioSource>();
        }

        protected void Start()
        {
            // 从本地缓存读取声音音量
            if (PlayerPrefs.HasKey(musicVolumePrefs))
            {
                musicVolume = PlayerPrefs.GetFloat(musicVolumePrefs);
            }
            if (PlayerPrefs.HasKey(soundVolumePrefs))
            {
                musicVolume = PlayerPrefs.GetFloat(soundVolumePrefs);
            }
        }

        public void Initialize()
        {
            LoadClips();
        }
        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="id"></param>
        /// <param name="loop"></param>
        public AudioSource PlayMusic(string id, bool loop = true, int type = 2, float volume = 1f)
        {
            musicAudioSource.clip = GetAudioClip(id, type);
            musicAudioSource.clip.LoadAudioData();
            musicAudioSource.loop = loop;
            musicAudioSource.Play();
            CommonUtils.StartProcedure(SKCurve.LinearIn, 0.5f, (f) =>
            {
                musicAudioSource.volume = f * musicVolume;
            });
            return musicAudioSource;
        }
        public AudioSource StopMusic()
        {
            float oVolume = musicAudioSource.volume;
            CommonUtils.StartProcedure(SKCurve.LinearIn, 0.5f, (f) =>
            {
                musicAudioSource.volume = oVolume * (1 - f);
            });
            return musicAudioSource;
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="id"></param>
        /// <param name="loop"></param>
        public void ChangeMusic(bool change_bgm = false)
        {
            if (musicAudioSource.isPlaying)
            {
                musicAudioSource.Stop();
                BGMhasPlayed = (BGMhasPlayed + 1) % BGMs.Count;
                PlayMusic(BGMs[BGMhasPlayed]);
            }
            else if (change_bgm == true)
            {
                BGMhasPlayed = (BGMhasPlayed + 1) % BGMs.Count;
            }
            else
            {
                PlayMusic(BGMs[BGMhasPlayed]);
            }
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="id"></param>
        public AudioSource PlaySound(string id, Action action = null, bool loop = false, float volume = 1f)
        {
            string blackId = "";
            if (blackSoundDic.ContainsKey(id))
            {
                if (blackSoundDic[id] >= blackCount)
                {
                    return null;
                }
                else
                {
                    blackSoundDic[id]++;
                    blackId = id;
                }
            }
            if (unusedSoundAudioSourceList.Count != 0)
            {

                audioSource = UnusedToUsed();
                audioSource.clip = GetAudioClip(id, 1);
                audioSource.clip.LoadAudioData();
                audioSource.volume = soundVolume * volume;
                audioSource.Play();

                StartCoroutine(WaitPlayEnd(audioSource, action, blackId));
            }
            else
            {
                AddAudioSource();
                audioSource = UnusedToUsed();
                audioSource.clip = GetAudioClip(id, 1);
                audioSource.clip.LoadAudioData();
                audioSource.volume = soundVolume * volume;
                audioSource.loop = loop;
                audioSource.Play();

                StartCoroutine(WaitPlayEnd(audioSource, action, blackId));
            }
            return audioSource;
        }

        public AudioSource PlayIdentifiableSound(string fileName, string id, bool loop = false, float volume = 1)
        {
            if (audioSourceDict.ContainsKey(id))
            {
                return null;
                //StopIdentifiableSound(id);
            }

            AudioSource audioSource = AddIdentifiableAudioSource(id);
            audioSource.clip = GetAudioClip(fileName, 1);
            audioSource.clip.LoadAudioData();
            audioSource.loop = loop;
            audioSource.volume = soundVolume * volume;
            audioSource.Play();

            return audioSource;
        }

        public void StopIdentifiableSound(string id, float dampTime = 0.15f)
        {
            RemoveIdentifiableAudioSource(id, dampTime);
        }

        public AudioSource PlayMusicBar(string id, Action action = null)
        {
            if (unusedSoundAudioSourceList.Count != 0)
            {
                MBsource = UnusedToUsed();
                MBsource.clip = GetAudioClip(id, 1);
                MBsource.clip.LoadAudioData();
                MBsource.Play();

                StartCoroutine(WaitPlayEnd(MBsource, action));
            }
            else
            {
                AddAudioSource();

                MBsource = UnusedToUsed();
                MBsource.clip = GetAudioClip(id, 1);
                MBsource.clip.LoadAudioData();
                MBsource.volume = soundVolume * 0.75f;
                MBsource.loop = false;
                MBsource.Play();

                StartCoroutine(WaitPlayEnd(MBsource, action));
            }
            return MBsource;
        }
        /// <summary>
        /// 停播音效
        /// </summary>
        /// <param name="id"></param>
        public void StopSound()
        {
            CommonUtils.EditorLogNormal(audioSource.GetInstanceID());

            if (audioSource != null)
            {
                audioSource.Stop();
            }
            
        }

        /// <summary>
        /// 播放3d音效
        /// </summary>
        /// <param name="id"></param>
        /// <param name="position"></param>
        public AudioSource Play3dSound(string id, Vector3 position, bool loop =false, float volume = 1f)
        {
            if (unusedSoundAudioSourceList.Count != 0)
            {
                audioSource = UnusedToUsed();
                audioSource.clip = GetAudioClip(id, 1);
                audioSource.clip.LoadAudioData();
                AudioSource.PlayClipAtPoint(audioSource.clip, position);

                StartCoroutine(WaitPlayEnd(audioSource, null));
            }
            else
            {
                AddAudioSource();

                audioSource = UnusedToUsed();
                audioSource.clip = GetAudioClip(id, 1);
                audioSource.clip.LoadAudioData();
                audioSource.volume = soundVolume * volume;
                audioSource.loop = loop;
                AudioSource.PlayClipAtPoint(audioSource.clip, position);

                StartCoroutine(WaitPlayEnd(audioSource, null));
            }
            //audioSource.clip = GetAudioClip(id);
            //AudioSource.PlayClipAtPoint(audioSource.clip, position);   
            return audioSource;
        }

        /// <summary>
        /// 当播放音效结束后，将其移至未使用集合
        /// </summary>
        /// <param name="audioSource"></param>
        /// <returns></returns>
        IEnumerator WaitPlayEnd(AudioSource audioSource, Action action, string blackId = "")
        {
            yield return new WaitUntil(() => { return !audioSource.isPlaying; });
            if (!string.IsNullOrEmpty(blackId))
            {
                blackSoundDic[blackId]--;
            }
            UsedToUnused(audioSource);
            if (action != null)
            {
                action();
            }
        }

        /// <summary>
        /// 获取音频文件，获取后会缓存一份
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private AudioClip GetAudioClip(string id, int Type)
        {
            if (!audioClipDict.ContainsKey(id))
            {
                // if (!audioPathDict.ContainsKey(id))
                //     return null;
                string path = null;
                switch (Type)
                {
                    case 0:
                        path = MUSIC_PATH;
                        break;
                    case 1:
                        path = SOUND_PATH;
                        break;
                    case 2:
                        path = BGM_PATH;
                        break;
                }
                AudioClip ac = Resources.Load(path + id) as AudioClip;
                CommonUtils.EditorLogNormal("Loadmusic:" + id);
                if (ac == null)
                {
                    CommonUtils.EditorLogError(path + id + "这个音乐资源有问题");
                }
                audioClipDict.Add(id, ac);
            }
            return audioClipDict[id];
        }

        private void LoadClips()
        {
            var keys = TableAgent.instance.CollectKey1("AudioClip");
            foreach (var key in keys)
            {
                int type = TableAgent.instance.GetInt("AudioClip", key.ToString(), "Type");
                if (type == 2)
                {
                    GetAudioClip(key, type);
                    BGMs.Add(key);
                }
            }
            GetMusic_list(keys);
            foreach (var music_id in music_ids)
            {
                GetAudioClip(music_id, 0);
            }
            //foreach (string id in music_ids)
            //{
            //  //  CommonUtils.EditorLog("id:"+id);
            //}
        }

        private void GetMusic_list(List<string> keys)
        {
            List<string> music_list = new List<string>();
            foreach (var key in keys)
            {
                int type = TableAgent.instance.GetInt("AudioClip", key.ToString(), "Type");
                if (type == 0)
                {
                    music_list.Add(key);
                }
            }
            int count = music_list.Count;
            for (int i = 0; i < count; i++)
            {
                int ran = UnityEngine.Random.Range(0, music_list.Count - 1);
                music_ids.Add(music_list[ran]);
                music_list.RemoveAt(ran);
            }
        }

        /// <summary>
        /// 添加音频组件
        /// </summary>
        /// <returns></returns>
        private AudioSource AddAudioSource()
        {
            if (unusedSoundAudioSourceList.Count != 0)
            {
                return UnusedToUsed();
            }
            else
            {
                AudioSource audioSource = gameObject.AddComponent<AudioSource>();
                unusedSoundAudioSourceList.Add(audioSource);
                return audioSource;
            }
        }

        private AudioSource AddIdentifiableAudioSource(string id)
        {
            GameObject go = new GameObject("AudioAgent");
            AudioSource audioSource = go.AddComponent<AudioSource>();
            CommonUtils.InsertOrUpdateKeyValueInDictionary(audioSourceDict, id, audioSource);

            return audioSource;
        }

        private void RemoveIdentifiableAudioSource(string id, float dampenTime = 0.15f)
        {
            if (!audioSourceDict.ContainsKey(id))
                return;
            AudioSource audioSource = audioSourceDict[id];
            CommonUtils.RemoveKeyInDictionary(audioSourceDict, id);

            float oVolume = audioSource.volume;
            CommonUtils.StartProcedure(SKCurve.LinearIn, dampenTime, (f) =>
            {
                audioSource.volume = oVolume * (1 - f);
            }, (f) =>
            {
                Destroy(audioSource.gameObject);
            });
        }

        /// <summary>
        /// 将未使用的音频组件移至已使用集合里
        /// </summary>
        /// <returns></returns>
        private AudioSource UnusedToUsed()
        {
            AudioSource audioSource = unusedSoundAudioSourceList[0];
            unusedSoundAudioSourceList.RemoveAt(0);
            usedSoundAudioSourceList.Add(audioSource);
            return audioSource;
        }

        /// <summary>
        /// 将使用完的音频组件移至未使用集合里
        /// </summary>
        /// <param name="audioSource"></param>
        private void UsedToUnused(AudioSource audioSource)
        {
            if (usedSoundAudioSourceList.Contains(audioSource))
            {
                usedSoundAudioSourceList.Remove(audioSource);
            }
            if (unusedSoundAudioSourceList.Count >= poolCount)
            {
                Destroy(audioSource);
            }
            else if (audioSource != null && !unusedSoundAudioSourceList.Contains(audioSource))
            {
                unusedSoundAudioSourceList.Add(audioSource);
            }
        }

        /// <summary>
        /// 修改背景音乐音量
        /// </summary>
        /// <param name="volume"></param>
        public void ChangeMusicVolume(float volume)
        {
            musicVolume = volume;
            musicAudioSource.volume = volume;

            PlayerPrefs.SetFloat(musicVolumePrefs, volume);
        }

        /// <summary>
        /// 修改音效音量
        /// </summary>
        /// <param name="volume"></param>
        public void ChangeSoundVolume(float volume)
        {
            soundVolume = volume;
            for (int i = 0; i < unusedSoundAudioSourceList.Count; i++)
            {
                unusedSoundAudioSourceList[i].volume = volume;
            }
            for (int i = 0; i < usedSoundAudioSourceList.Count; i++)
            {
                usedSoundAudioSourceList[i].volume = volume;
            }

            PlayerPrefs.SetFloat(soundVolumePrefs, volume);
        }
    }
}