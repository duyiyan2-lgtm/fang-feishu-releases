import dayjs from 'dayjs'
import 'dayjs/locale/zh-cn'
import isBetween from 'dayjs/plugin/isBetween'
import weekOfYear from 'dayjs/plugin/weekOfYear'

dayjs.extend(isBetween)
dayjs.extend(weekOfYear)
dayjs.locale('zh-cn')

export default dayjs

export const formatDate = (date, fmt = 'YYYY-MM-DD') => dayjs(date).format(fmt)
export const formatTime = (date, fmt = 'HH:mm') => dayjs(date).format(fmt)
export const formatDateTime = (date) => dayjs(date).format('YYYY-MM-DD HH:mm')
